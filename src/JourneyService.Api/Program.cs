using FluentValidation;
using JourneyService.Api.Hubs;
using JourneyService.Api.Observability;
using JourneyService.Application.Common.Interfaces;
using JourneyService.Application.Jorneys.Exceptions;
using JourneyService.Application.Journeys.Commands;
using JourneyService.Application.Journeys.Validators;
using JourneyService.Domain.Entities;
using JourneyService.Infrastructure.Messaging;
using JourneyService.Infrastructure.Persistence;
using JourneyService.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Prometheus;
using Serilog;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());
var frontendBaseUrl = builder.Configuration["Authentication:Microsoft:FrontendBaseUrl"]?.TrimEnd('/')
    ?? "http://localhost:4200";
var tenantId = builder.Configuration["Authentication:Microsoft:TenantId"];
var clientId = builder.Configuration["Authentication:Microsoft:ClientId"];
var clientSecret = builder.Configuration["Authentication:Microsoft:ClientSecret"];
var authority = builder.Configuration["Authentication:Microsoft:Authority"];
var postLogoutRedirectUri = builder.Configuration["Authentication:Microsoft:PostLogoutRedirectUri"]
    ?? $"{frontendBaseUrl}/auth/login";

builder.Services.AddSignalR();
builder.Services.AddScoped<IHubNotifier, HubNotifier>();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<DbLatencyMetricsInterceptor>();

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
//builder.Services.AddDbContext<ApplicationDbContext>(options =>
//    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
           .AddInterceptors(sp.GetRequiredService<DbLatencyMetricsInterceptor>())
           .AddInterceptors(new ConvertDomainEventsToOutboxMessagesInterceptor());
});

builder.Services.AddScoped<IApplicationDbContext>(provider =>
    provider.GetRequiredService<ApplicationDbContext>());

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("JourneyService.Api"))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddSqlClientInstrumentation()
            .AddZipkinExporter(options =>
            {
                options.Endpoint = new Uri(
                    builder.Configuration["OpenTelemetry:ZipkinEndpoint"]
                    ?? "http://zipkin:9411/api/v2/spans");
            });
    });

builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: new[] { "live" })
    .AddDbContextCheck<ApplicationDbContext>("sql", tags: new[] { "ready" })
    .AddCheck<RabbitMqHealthCheck>("rabbitmq", tags: new[] { "ready" });

builder.Services.AddHostedService<BrokerLagMetricsService>();

builder.Services.AddAuthentication(options => {
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options => {
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;

    options.Events = new CookieAuthenticationEvents
    {
        OnRedirectToLogin = context => {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        },

        // REQUIREMENT U-1: Silent Session Resume (Refresh Token Exchange)
        OnValidatePrincipal = async context =>
        {
            var now = DateTimeOffset.UtcNow;
            var expiresAtValue = context.Properties.GetTokenValue("expires_at");
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>(); 

            if (DateTimeOffset.TryParse(expiresAtValue, out var expiresAt) && expiresAt < now)
            {
                var refreshToken = context.Properties.GetTokenValue("refresh_token");
                if (string.IsNullOrEmpty(refreshToken))
                {
                    logger.LogWarning("Session expired and no refresh token available. CorrelationId: {CorrelationId}",
                        context.HttpContext.TraceIdentifier);
                    context.RejectPrincipal();
                    return;
                }
                if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
                {
                    logger.LogWarning("Microsoft authentication settings are incomplete. CorrelationId: {CorrelationId}",
                        context.HttpContext.TraceIdentifier);
                    context.RejectPrincipal();
                    return;
                }

                var tokenEndpoint = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token";

                using var client = new HttpClient();
                var requestBody = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    {"client_id", clientId ?? string.Empty},
                    {"client_secret", clientSecret ?? string.Empty},
                    {"grant_type", "refresh_token"},
                    {"refresh_token", refreshToken}
                });

                var response = await client.PostAsync(tokenEndpoint, requestBody);
                if (response.IsSuccessStatusCode)
                {
                    var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                    var newAccessToken = json.RootElement.GetProperty("access_token").GetString();
                    var newRefreshToken = json.RootElement.GetProperty("refresh_token").GetString();
                    var expiresIn = json.RootElement.GetProperty("expires_in").GetInt32();

                    // Update tokens in the cookie
                    context.Properties.UpdateTokenValue("access_token", newAccessToken);
                    context.Properties.UpdateTokenValue("refresh_token", newRefreshToken);
                    context.Properties.UpdateTokenValue("expires_at", DateTimeOffset.UtcNow.AddSeconds(expiresIn).ToString("o"));
                   
                    context.ShouldRenew = true; // Tell middleware to update the cookie

                    logger.LogInformation("Token successfully refreshed silently. CorrelationId: {CorrelationId}",
                       context.HttpContext.TraceIdentifier);
                }
                else
                {

                    var errorDetails = await response.Content.ReadAsStringAsync();
                    logger.LogError("Silent token exchange failed. Status: {StatusCode}, Error: {Error}, CorrelationId: {CorrelationId}",
                        response.StatusCode, errorDetails, context.HttpContext.TraceIdentifier);
                    context.RejectPrincipal(); // Token invalid/revoked
                    
                }
            }
        }
    };
})
.AddOpenIdConnect(options => {
    options.Authority = authority;

    options.ClientId = clientId;

    options.ClientSecret = clientSecret;


    options.UsePkce = true; // REQUIREMENT U-1
    options.ResponseType = OpenIdConnectResponseType.Code;
    options.SaveTokens = true; // Required to store tokens in the cookie

    // REQUIREMENT U-1: Ask for offline_access to get a Refresh Token
    options.Scope.Clear();
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("offline_access");
    //options.SignedOutCallbackPath = "/signout-callback-oidc";
    options.Events = new OpenIdConnectEvents
    {

        OnRemoteFailure = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError(context.Failure, "Remote authentication failure. CorrelationId: {CorrelationId}",
                context.HttpContext.TraceIdentifier);

            var error = Uri.EscapeDataString(context.Failure?.Message ?? "Authentication failed");
            context.Response.Redirect($"{frontendBaseUrl}/auth/login?error={error}");
            context.HandleResponse();
            return Task.CompletedTask;
        },

        OnTokenValidated = async context =>
        {
            var db = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();

           
            var userId = context.Principal?.FindFirstValue("oid")
                         ?? context.Principal?.FindFirstValue("sub");

            var email = context.Principal?.FindFirstValue("preferred_username")
                        ?? context.Principal?.FindFirstValue("email");

            if (string.IsNullOrEmpty(userId))
            {
                context.Fail("User Identifier (oid/sub) is missing from the provider.");
                return;
            }
            var user = await db.Users.FindAsync(userId);
            if (user != null && user.Status != UserStatus.Active)
            {
                context.Fail($"Account is {user.Status}. Contact admin.");
            }

            // 3. Check if user exists in our local User table
            var localUser = await db.Users.FindAsync(userId);

            if (localUser == null)
            {
                
                localUser = new UserProfile
                {
                    Id = userId,
                    Email = email,
                    Status = UserStatus.Active,
                    CreatedAt = DateTime.UtcNow
                };
                db.Users.Add(localUser);
                await db.SaveChangesAsync();
            }
            else if (localUser.Status != UserStatus.Active)
            {
                
                context.Fail($"Your account is {localUser.Status}. Please contact an administrator.");
            }
        },
        // REQUIREMENT U-6: Federated Sign-out
        OnRedirectToIdentityProviderForSignOut = context =>
        {
            context.ProtocolMessage.Prompt = "login";
            context.ProtocolMessage.PostLogoutRedirectUri = postLogoutRedirectUri;

            return Task.CompletedTask;
        }
    };

    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        NameClaimType = "name", // Or "preferred_username" depending on Azure config
        RoleClaimType = "roles",
        ValidateIssuer = true
    };

    options.MapInboundClaims = false;

});

// Dependency Injections
builder.Services.AddOpenApi();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddCors(options => {
    options.AddPolicy("AngularClient", policy => {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("login-limit", httpContext =>
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ip,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminScope", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireAssertion(context =>
            context.User.IsInRole("Admin") ||
            context.User.Claims.Any(c =>
                c.Type == "roles" &&
                string.Equals(c.Value, "Admin", StringComparison.OrdinalIgnoreCase)) ||
            context.User.Claims.Any(c =>
                (c.Type == "scope" || c.Type == "scp") &&
                c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Any(scope =>
                        string.Equals(scope, "Admin", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(scope, "admin", StringComparison.OrdinalIgnoreCase))));
    });
});
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(JourneyService.Application.Journeys.Commands.CreateJourneyCommand).Assembly));
builder.Services.AddValidatorsFromAssembly(typeof(CreateJourneyValidator).Assembly);
builder.Services.AddHttpContextAccessor(); //per te marr perd ClaimsPrincipal
builder.Services.AddSingleton<IEventBusPublisher, RabbitMqPublisher>();
builder.Services.AddHostedService<ProcessOutboxJob>();
builder.Services.AddAutoMapper(cfg => { }, typeof(CreateJourneyCommand).Assembly);
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddTransient<IEmailService, EmailService>();
builder.Services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();
var app = builder.Build();


app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var correlationId = context.TraceIdentifier;
        var exceptionFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        var exception = exceptionFeature?.Error;

        var (statusCode, message) = exception switch
        {
            ValidationException validationException => (
                StatusCodes.Status400BadRequest,
                string.Join("; ", validationException.Errors.Select(e => e.ErrorMessage).Distinct())
            ),
            ForbiddenAccessException forbiddenException => (StatusCodes.Status403Forbidden, forbiddenException.Message),
            UnauthorizedAccessException unauthorizedAccessException => (StatusCodes.Status403Forbidden, unauthorizedAccessException.Message),
            KeyNotFoundException keyNotFoundException => (StatusCodes.Status404NotFound, keyNotFoundException.Message),
            _ => (StatusCodes.Status500InternalServerError, "An internal server error occurred.")
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsJsonAsync(new
        {
            Error = message,
            CorrelationId = correlationId
        });
    });
});

//Middleware Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.MapOpenApi();
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging();
app.UseHttpMetrics();

app.UseCors("AngularClient");


app.UseAuthentication();
app.UseAuthorization();



app.MapControllers();
app.MapMetrics("/metrics");
app.MapHealthChecks("/healthz", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});
app.MapHealthChecks("/readyz", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});



// Migration ne startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    const int maxAttempts = 10;
    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            db.Database.Migrate();
            logger.LogInformation("Database migration completed.");
            break;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            logger.LogWarning(ex, "Database migration attempt {Attempt}/{MaxAttempts} failed. Retrying...", attempt, maxAttempts);
            await Task.Delay(TimeSpan.FromSeconds(5));
        }
    }
}
app.MapHub<JourneyHub>("/hubs/journey");
app.Run();

