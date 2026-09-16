namespace PodcastApi.Sync;

public class ScheduledSyncService(
    IServiceProvider services,
    IConfiguration cfg,
    ILogger<ScheduledSyncService> logger) : BackgroundService
{
    private static readonly DayOfWeek[] DefaultDays = [DayOfWeek.Tuesday];
    private const int DefaultIntervalHours = 3;
    private const string DefaultTimeZone = "Europe/Madrid";

    public static DateTime NextRunLocal(
        DateTime nowLocal, IReadOnlyCollection<DayOfWeek> days, int intervalHours)
    {
        if (days.Count == 0) throw new ArgumentException("No sync day configured.", nameof(days));
        if (intervalHours is < 1 or > 24)
            throw new ArgumentOutOfRangeException(nameof(intervalHours), intervalHours, "Must be 1-24.");

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

        if (cfg.GetValue<bool?>("Sync:RunAtStartup") ?? true)
            await RunOnceAsync("startup", stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var nowLocal = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, tz);
            var nextLocal = NextRunLocal(nowLocal.DateTime, days, intervalHours);
            var nextUtc = new DateTimeOffset(nextLocal, tz.GetUtcOffset(nextLocal)).ToUniversalTime();
            var delay = nextUtc - DateTimeOffset.UtcNow;

            if (delay > TimeSpan.Zero)
            {
                try
                {
                    await Task.Delay(delay, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            await RunOnceAsync("scheduled", stoppingToken);
        }
    }

    private async Task RunOnceAsync(string trigger, CancellationToken ct)
    {
        try
        {
            using var scope = services.CreateScope();
            var sync = scope.ServiceProvider.GetRequiredService<SyncService>();
            var result = await sync.SyncAsync(ct);

            logger.LogInformation("Refresh [{Trigger}]: {FeedItems} items, {Added} added, {Updated} updated.",
                trigger, result.FeedItems, result.Added, result.Updated);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Refresh [{Trigger}] failed.", trigger);
        }
    }

    private DayOfWeek[] ReadDays()
    {
        var raw = cfg["Sync:Days"];
        if (string.IsNullOrWhiteSpace(raw)) return DefaultDays;

        var days = new List<DayOfWeek>();
        foreach (var part in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (Enum.TryParse<DayOfWeek>(part, ignoreCase: true, out var day) && !days.Contains(day))
                days.Add(day);
        }

        return days.Count != 0 ? [.. days] : DefaultDays;
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
            logger.LogError(ex, "Time zone '{TimeZone}' not found; using UTC.", id);
            return TimeZoneInfo.Utc;
        }
    }
}
