
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using System.Security.Claims;


[ApiController]
[Route("api/auth")]
public class AccountController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public AccountController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    //[EnableRateLimiting("login-limit")]
    //[HttpGet("login")]
    //public IActionResult Login(string returnUrl = "/")
    //{
    //    var props = new AuthenticationProperties
    //    {
    //        RedirectUri = returnUrl,
    //        IsPersistent = true // Important for "Keep me signed in"
    //    };
    //    props.Items.Add("prompt", "select_account");

    //    return Challenge(props, OpenIdConnectDefaults.AuthenticationScheme);
    //}
    [EnableRateLimiting("login-limit")]
    [HttpGet("login")]
    public IActionResult Login(string returnUrl = "/")
    {
        var props = new AuthenticationProperties
        {
            RedirectUri = returnUrl,
            IsPersistent = true
        };
        return Challenge(props, OpenIdConnectDefaults.AuthenticationScheme);
    }

    //[HttpPost("logout")] 
    //[Authorize]
    //public IActionResult Logout()
    //{
    //    var isAuthenticated = User.Identity?.IsAuthenticated ?? false;

    //    // 2. Extract all claims into a list for easy viewing in the Debugger
    //    var userClaims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
    //    var callbackUrl = "http://localhost:4200/auth/login";

    //    return SignOut(
    //        new AuthenticationProperties { RedirectUri = callbackUrl },
    //        CookieAuthenticationDefaults.AuthenticationScheme,
    //        OpenIdConnectDefaults.AuthenticationScheme);


    //}

    [HttpGet("logout")] 

    public async Task Logout()
    {
        // 1. Clear the local app cookie
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        var redirectUri = _configuration["Authentication:Microsoft:SignedOutCallbackPath"];
        var prop = new AuthenticationProperties
        {
           //RedirectUri = "http://localhost:4200/auth/login"
           // RedirectUri = "http://localhost:5122/api/auth/signed-out"
            RedirectUri = redirectUri


        };

        await HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme, prop);
    }

    [HttpGet("signed-out")]
    public IActionResult SignedOut()
    {

        var redirectUri = _configuration["Authentication:Microsoft:PostLogoutRedirectUri"];
       // var spaRedirect = $"http://localhost:4200{returnUrl}";
        // return Redirect("http://localhost:4200/auth/login");

        return Redirect(redirectUri);

    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult GetCurrentUserId()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("oid")?.Value;

        var userData = new
        {
            Id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
             ?? User.FindFirst("oid")?.Value, 
            Email = User.FindFirst(ClaimTypes.Email)?.Value,
            Roles = User.FindAll("roles").Select(c => c.Value).ToList(),
            Scopes = User.FindAll("scope")
                .Concat(User.FindAll("scp"))
                .SelectMany(c => c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()
        };

        return Ok(userData);

       
    }





}
