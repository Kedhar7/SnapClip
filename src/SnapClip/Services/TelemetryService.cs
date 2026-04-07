using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SnapClip.Data;
using SnapClip.Models;

namespace SnapClip.Services;

/// <summary>
/// Local-only telemetry service for tracking usage patterns.
/// All data stays on the user's machine — nothing is transmitted externally.
/// </summary>
public sealed class TelemetryService
{
    private readonly IDbContextFactory<SnapClipDbContext> _contextFactory;

    public TelemetryService(IDbContextFactory<SnapClipDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    /// <summary>
    /// Logs a telemetry event with optional metadata.
    /// </summary>
    public async Task LogEventAsync(string eventName, object? eventData = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        var telemetryEvent = new TelemetryEvent
        {
            EventName = eventName,
            EventData = eventData is not null ? JsonSerializer.Serialize(eventData) : null,
            Timestamp = DateTime.UtcNow
        };

        context.TelemetryEvents.Add(telemetryEvent);
        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Gets events within a date range.
    /// </summary>
    public async Task<List<TelemetryEvent>> GetEventsByDateRangeAsync(DateTime from, DateTime to)
    {
        await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        return await context.TelemetryEvents
            .Where(e => e.Timestamp >= from && e.Timestamp <= to)
            .OrderByDescending(e => e.Timestamp)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the count of events by name for today.
    /// </summary>
    public async Task<int> GetTodayCountAsync(string eventName)
    {
        var today = DateTime.UtcNow.Date;
        await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        return await context.TelemetryEvents
            .CountAsync(e => e.EventName == eventName && e.Timestamp >= today)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the count of events by name for this week.
    /// </summary>
    public async Task<int> GetWeekCountAsync(string eventName)
    {
        var weekStart = DateTime.UtcNow.Date.AddDays(-(int)DateTime.UtcNow.DayOfWeek);
        await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        return await context.TelemetryEvents
            .CountAsync(e => e.EventName == eventName && e.Timestamp >= weekStart)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Gets usage distribution by hour of day (0-23).
    /// </summary>
    public async Task<Dictionary<int, int>> GetUsageByHourAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        var events = await context.TelemetryEvents
            .Select(e => e.Timestamp.Hour)
            .ToListAsync()
            .ConfigureAwait(false);

        return events
            .GroupBy(h => h)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    /// <summary>
    /// Gets the most recently logged events.
    /// </summary>
    public async Task<List<TelemetryEvent>> GetRecentEventsAsync(int count = 50)
    {
        await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        return await context.TelemetryEvents
            .OrderByDescending(e => e.Timestamp)
            .Take(count)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Gets total event count.
    /// </summary>
    public async Task<int> GetTotalEventCountAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        return await context.TelemetryEvents.CountAsync().ConfigureAwait(false);
    }
}
