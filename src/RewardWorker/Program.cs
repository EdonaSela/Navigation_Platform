using JourneyService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using RewardWorker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
           .AddInterceptors(new ConvertDomainEventsToOutboxMessagesInterceptor());
});

builder.Services.AddScoped<DailyGoalEvaluator>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
