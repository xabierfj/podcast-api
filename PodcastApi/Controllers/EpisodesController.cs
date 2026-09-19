using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PodcastApi.Data;
using PodcastApi.Dto;
using PodcastApi.Mappers;

namespace PodcastApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EpisodesController(AppDbContext db) : ControllerBase
{
    private const int MaxPageSize = 100;

    // Newest first; specials (no episode number) sort to the end.
    [HttpGet]
    public async Task<ActionResult<PagedResults<EpisodeDto>>> GetAll(int page = 1, int pageSize = 20)
    {
        if (page < 1) page = 1;
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var ordered = db.Episodes.NewestFirst();
        var totalCount = await ordered.CountAsync();
        var episodes = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(e => e.Guests)
            .ToListAsync();
        var items = episodes.Select(e => e.ToEpisodeDto()).ToList();
        return Ok(new PagedResults<EpisodeDto>(items, totalCount, page, pageSize));
    }

    // Most recently published, specials included.
    [HttpGet("latest")]
    public async Task<ActionResult<EpisodeDto>> GetLatest()
    {
        var episode = await db.Episodes
            .OrderByDescending(e => e.PublicationDate)
            .Include(e => e.Guests)
            .FirstOrDefaultAsync();

        return episode is null ? NotFound() : Ok(episode.ToEpisodeDto());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EpisodeDto>> GetById(int id)
    {
        var episode = await db.Episodes
            .Include(e => e.Guests)
            .FirstOrDefaultAsync(e => e.Id == id);
        
        return episode is null ? NotFound() : Ok(episode.ToEpisodeDto());
    }
}