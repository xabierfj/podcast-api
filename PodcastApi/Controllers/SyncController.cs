using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PodcastApi.Filters;
using PodcastApi.Sync;

namespace PodcastApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[ApiKey]
public class SyncController(SyncService sync, ILogger<SyncController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Post(CancellationToken ct)
    {
        // Same shape as the scheduler's line so one grep covers every refresh,
        // whoever triggered it.
        var startedAt = DateTimeOffset.Now;
        var stopwatch = Stopwatch.StartNew();

        var result = await sync.SyncAsync(ct);

        logger.LogInformation(
            "Refresh [manual] started {StartedAt:yyyy-MM-dd HH:mm:ss zzz}, took {Elapsed:n1}s: "
            + "{FeedItems} feed items, {Added} added, {Updated} updated.",
            startedAt, stopwatch.Elapsed.TotalSeconds,
            result.FeedItems, result.Added, result.Updated);

        return Ok(result);
    }
}
