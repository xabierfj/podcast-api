using Microsoft.AspNetCore.Mvc;
using PodcastApi.Filters;
using PodcastApi.Sync;

namespace PodcastApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[ApiKey]
public class SyncController(SyncService sync) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Post(CancellationToken ct) => Ok(await sync.SyncAsync("manual", ct));
}
