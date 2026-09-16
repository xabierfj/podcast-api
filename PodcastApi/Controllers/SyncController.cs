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
        var result = await sync.SyncAsync(ct);

        logger.LogInformation("Refresh [manual]: {FeedItems} items, {Added} added, {Updated} updated.",
            result.FeedItems, result.Added, result.Updated);

        return Ok(result);
    }
}
