using System.Diagnostics;

namespace PodcastApi.Sync;

/// <summary>
/// Background job that runs <see cref="SyncService"/> on the days the podcast
/// publishes (Sync:Days, default Tuesday), repeating every Sync:IntervalHours
/// within those days and idling the rest of the week.
///
/// Slots are aligned to wall-clock hours in Sync:TimeZone (00:00, 03:00, ...),
/// not to process start, so restarting the app does not shift the schedule.
/// Each run resolves a fresh scope because <see cref="SyncService"/> depends on
/// the scoped DbContext.
/// </summary>
public class ScheduledSyncService(
    IServiceProvider services,
    IConfiguration cfg,
    ILogger<ScheduledSyncService> logger) : BackgroundService
{
    private static readonly DayOfWeek[] DefaultDays = [DayOfWeek.Tuesday];
    private const int DefaultIntervalHours = 3;
    private const string DefaultTimeZone = "Europe/Madrid";

    /// <summary>
    /// Next slot strictly after <paramref name="nowLocal"/>, where slots are at
    /// 00:00 + n * <paramref name="intervalHours"/> on each day in
    /// <paramref name="days"/>. Pure and wall-clock based so it can be tested.
    /// </summary>
    public static DateTime NextRunLocal(
        DateTime nowLocal, IReadOnlyCollection<DayOfWeek> days, int intervalHours)
    {
        if (days.Count == 0) throw new ArgumentException("No sync day configured.", nameof(days));
        if (intervalHours is < 1 or > 24)
            throw new ArgumentOutOfRangeException(nameof(intervalHours), intervalHours, "Must be 1-24.");

        // 0..7 so that "today, later" and "same weekday next week" are both covered.
        for (var dayOffset = 0; dayOffset <= 7; dayOffset++)
        {
            var date = nowLocal.Date.AddDays(dayOffset);
            if (!days.Contains(date.DayOfWeek)) continue;

            for (var hour = 0; hour < 24; hour += intervalHours)
            {
                var slot = date.AddHours(hour);
                if (slot > nowLocal) return slot;
            }
        }

        throw new InvalidOperationException("No slot found within 7 days.");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var days = ReadDays();
        var intervalHours = Math.Clamp(cfg.GetValue<int?>("Sync:IntervalHours") ?? DefaultIntervalHours, 1, 24);
        var tz = ReadTimeZone();

        logger.LogInformation(
            "Scheduled sync: {Days} every {Interval}h ({TimeZone}).",
            string.Join(", ", days), intervalHours, tz.Id);

        // Catch up immediately: a container started mid-week would otherwise sit
        // idle until the next publish day with a possibly empty database.
        if (cfg.GetValue<bool?>("Sync:RunAtStartup") ?? true)
            await RunOnceAsync("startup", stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var nowLocal = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, tz);
            var nextLocal = NextRunLocal(nowLocal.DateTime, days, intervalHours);

            // Resolve the offset at the target instant so DST shifts don't drift the slot.
            var nextUtc = new DateTimeOffset(nextLocal, tz.GetUtcOffset(nextLocal)).ToUniversalTime();
            var delay = nextUtc - DateTimeOffset.UtcNow;

            if (delay > TimeSpan.Zero)
            {
                logger.LogInformation("Next sync at {Next:yyyy-MM-dd HH:mm} {TimeZone}.", nextLocal, tz.Id);
                try
                {
                    await Task.Delay(delay, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break; // shutting down
                }
            }

            await RunOnceAsync("scheduled", stoppingToken);
        }
    }

    private async Task RunOnceAsync(string trigger, CancellationToken ct)
    {
        var startedAt = DateTimeOffset.Now;
        var stopwatch = Stopwatch.StartNew();
        try
        {
            using var scope = services.CreateScope();
            var sync = scope.ServiceProvider.GetRequiredService<SyncService>();
            var result = await sync.SyncAsync(ct);

            logger.LogInformation(
                "Refresh [{Trigger}] started {StartedAt:yyyy-MM-dd HH:mm:ss zzz}, took {Elapsed:n1}s: "
                + "{FeedItems} feed items, {Added} added, {Updated} updated.",
                trigger, startedAt, stopwatch.Elapsed.TotalSeconds,
                result.FeedItems, result.Added, result.Updated);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // shutting down
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Refresh [{Trigger}] failed after {Elapsed:n1}s; will retry at the next slot.",
                trigger, stopwatch.Elapsed.TotalSeconds);
        }
    }

    // Comma-separated so it survives a single env var: Sync__Days=Tuesday,Wednesday
    private DayOfWeek[] ReadDays()
    {
        var raw = cfg["Sync:Days"];
        if (string.IsNullOrWhiteSpace(raw)) return DefaultDays;

        var days = new List<DayOfWeek>();
        foreach (var part in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (Enum.TryParse<DayOfWeek>(part, ignoreCase: true, out var day))
            {
                if (!days.Contains(day)) days.Add(day);
            }
            else
            {
                logger.LogWarning("Ignoring unrecognised Sync:Days entry '{Entry}'.", part);
            }
        }

        if (days.Count != 0) return [.. days];

        logger.LogWarning("Sync:Days had no valid entries; falling back to {Default}.", DefaultDays[0]);
        return DefaultDays;
    }

    private TimeZoneInfo ReadTimeZone()
    {
        var id = cfg["Sync:TimeZone"];
        if (string.IsNullOrWhiteSpace(id)) id = DefaultTimeZone;

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(id);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            // Container images without tzdata land here; UTC would silently shift
            // the Tuesday slots, so make it loud.
            logger.LogError(ex, "Time zone '{TimeZone}' not found; falling back to UTC. Slots will be offset.", id);
            return TimeZoneInfo.Utc;
        }
    }
}
