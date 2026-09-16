namespace PodcastApi.Sync;

/// <summary>
/// Background job that runs <see cref="SyncService"/> once at startup and then
/// on a fixed interval (Sync:IntervalHours, default 12). Each run resolves a
/// fresh scope because <see cref="SyncService"/> depends on the scoped DbContext.
/// </summary>
public class ScheduledSyncService(
    IServiceProvider services,
    IConfiguration cfg,
    ILogger<ScheduledSyncService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromHours(cfg.GetValue<double?>("Sync:IntervalHours") ?? 12);
        using var timer = new PeriodicTimer(interval);

        // do/while => run immediately, then wait one interval between runs.
        do
        {
            try
            {
                using var scope = services.CreateScope();
                var sync = scope.ServiceProvider.GetRequiredService<SyncService>();
                await sync.SyncAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break; // shutting down
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Scheduled sync failed; will retry next interval.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
