using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PodcastApi.Data;
using PodcastApi.Dto;
using PodcastApi.Mappers;

namespace PodcastApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GuestsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<GuestSummaryDto>>> GetAllGuests()
    {
        var guests = await db.Guests
            .OrderBy(g => g.Name)
            .Select(g => new GuestSummaryDto(g.Id, g.Name, g.Episodes.Count))
            .ToListAsync();
        return Ok(guests);
    }

    [HttpGet("{id:int}/episodes")]
    public async Task<ActionResult<GuestWithEpisodesDto>> GetEpisodesWithGuest(int id)
    {
        var guest = await db.Guests
            .Include(g => g.Episodes).ThenInclude(e => e.Guests)
            .FirstOrDefaultAsync(g => g.Id == id);
        if (guest is null) return NotFound();

        var episodes = guest.Episodes
            .OrderBy(e => e.EpisodeNumber == null)
            .ThenByDescending(e => e.EpisodeNumber)
            .Select(e => e.ToEpisodeDto())
            .ToList();
        return Ok(new GuestWithEpisodesDto() {GuestId = guest.Id, Name =  guest.Name, Episodes = episodes});
    }
}