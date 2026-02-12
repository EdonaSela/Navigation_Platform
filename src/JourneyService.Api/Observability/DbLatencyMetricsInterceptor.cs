using Microsoft.EntityFrameworkCore.Diagnostics;
using Prometheus;
using System.Data.Common;

namespace JourneyService.Api.Observability;

public sealed class DbLatencyMetricsInterceptor : DbCommandInterceptor
{
    private static readonly Histogram DbLatency = Metrics.CreateHistogram(
        "navigation_db_command_duration_seconds",
        "Database command latency in seconds.",
        new HistogramConfiguration
        {
            LabelNames = new[] { "command_type" },
            Buckets = Histogram.ExponentialBuckets(start: 0.001, factor: 2, count: 12)
        });

    public override DbDataReader ReaderExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result)
    {
        Observe(eventData.Duration, command.CommandText);
        return result;
    }

    public override object? ScalarExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        object? result)
    {
        Observe(eventData.Duration, command.CommandText);
        return result;
    }

    public override int NonQueryExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result)
    {
        Observe(eventData.Duration, command.CommandText);
        return result;
    }

    private static void Observe(TimeSpan duration, string? sql)
    {
        DbLatency
            .WithLabels(GetCommandType(sql))
            .Observe(duration.TotalSeconds);
    }

    private static string GetCommandType(string? sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            return "unknown";
        }

        var firstWord = sql.TrimStart()
            .Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault()?
            .ToUpperInvariant();

        return firstWord switch
        {
            "SELECT" => "select",
            "INSERT" => "insert",
            "UPDATE" => "update",
            "DELETE" => "delete",
            _ => "other"
        };
    }
}
