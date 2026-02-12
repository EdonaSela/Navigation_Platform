using System.Text.Json;
using JourneyService.Infrastructure.Messaging;
using JourneyService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using static JourneyService.Domain.Events.JourneyEvents;

namespace RewardWorker;

public sealed class DailyGoalEvaluator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<DailyGoalEvaluator> _logger;

    public DailyGoalEvaluator(ApplicationDbContext dbContext, ILogger<DailyGoalEvaluator> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task ProcessAsync(OutboxEventEnvelope envelope)
    {
        switch (envelope.Type)
        {
            case nameof(JourneyCreatedEvent):
            {
                var ev = JsonSerializer.Deserialize<JourneyCreatedEvent>(envelope.Content, JsonOptions);
                if (ev is not null) await EvaluateDailyGoal(ev.UserId, ev.Date);
                break;
            }
            case nameof(JourneyUpdatedEvent):
            {
                var ev = JsonSerializer.Deserialize<JourneyUpdatedEvent>(envelope.Content, JsonOptions);
                if (ev is not null) await EvaluateDailyGoal(ev.UserId, ev.NewDate);
                break;
            }
            case nameof(JourneyDeletedEvent):
            {
                var ev = JsonSerializer.Deserialize<JourneyDeletedEvent>(envelope.Content, JsonOptions);
                if (ev is not null) await EvaluateDailyGoal(ev.UserId, ev.Date);
                break;
            }
            default:
                _logger.LogDebug("RewardWorker ignored event type {EventType}.", envelope.Type);
                break;
        }
    }

    private async Task EvaluateDailyGoal(string userId, DateTime date)
    {
        var journeysToday = await _dbContext.Journeys
            .Where(j => j.UserId == userId && j.StartTime.Date == date.Date)
            .OrderBy(j => j.StartTime)
            .ToListAsync();

        if (!journeysToday.Any())
        {
            return;
        }

        var totalDistance = journeysToday.Sum(j => j.Distance.Value);

        if (totalDistance >= 20.00m)
        {
            if (!journeysToday.Any(j => j.IsDailyGoalAchieved))
            {
                journeysToday[0].MarkAsGoalAchiever();
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Daily goal achieved for user {UserId} on {Date}.", userId, date.Date);
            }
        }
        else
        {
            var achievedJourneys = journeysToday.Where(j => j.IsDailyGoalAchieved).ToList();
            if (achievedJourneys.Any())
            {
                foreach (var journey in achievedJourneys)
                {
                    journey.ResetGoal();
                }

                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Daily goal reset for user {UserId} on {Date}.", userId, date.Date);
            }
        }
    }
}

