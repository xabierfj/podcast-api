using Microsoft.EntityFrameworkCore;
using PodcastApi.Data;
using PodcastApi.Domain;
using PodcastApi.Parsing;
using PodcastApi.Rss;

namespace PodcastApi.Sync;

/// <summary>
/// Fetches the RSS feed, parses each item, and upserts the results into the
/// database. <see cref="Episode.AudioUrl"/> is the dedup key: an item whose
/// audio URL already exists is updated in place, otherwise it is inserted.
/// </summary>
public class SyncService(
    AppDbContext db,
    RssFeedService feed,
    EpisodeParser parser,
    ILogger<SyncService> logger)
{
    public async Task<SyncResult> SyncAsync(string trigger, CancellationToken ct = default)
    {
        var rssItems = await feed.GetEpisodesAsync();

        // Parse everything up front and drop items we can't dedup on. An empty
        // AudioUrl would collide on the unique index the moment a second one appears.
        var parsed = rssItems
            .Select(parser.ParseEpisode)
            .Where(p =>
            {
                if (!string.IsNullOrWhiteSpace(p.AudioUrl)) return true;
                logger.LogWarning("Skipping '{Title}': no audio URL (dedup key).", p.OriginalTitle);
                return false;
            })
            .ToList();

        // Pull the episodes already in the DB that this feed refers to, keyed by
        // the dedup key. Include the guests so EF tracks the join rows on update.
        var audioUrls = parsed.Select(p => p.AudioUrl!).ToList();
        var existing = await db.Episodes
            .Include(e => e.Guests)
            .Where(e => audioUrls.Contains(e.AudioUrl!))
            .ToDictionaryAsync(e => e.AudioUrl!, ct);

        // Guest.Name is unique, so every name must resolve to one tracked row.
        // Seed the cache with all existing guests, then add new ones as we meet them.
        var guestCache = await db.Guests
            .ToDictionaryAsync(g => g.Name, StringComparer.OrdinalIgnoreCase, ct);

        int added = 0, updated = 0;

        foreach (var p in parsed)
        {
            if (existing.TryGetValue(p.AudioUrl, out var episode))
            {
                MapInto(p, episode, guestCache);
                updated++;
            }
            else
            {
                episode = new Episode();
                MapInto(p, episode, guestCache);
                db.Episodes.Add(episode);
                added++;
            }
        }

        await db.SaveChangesAsync(ct);

        logger.LogInformation("Refresh [{Trigger}]: {FeedItems} items, {Added} added, {Updated} updated.",
            trigger, rssItems.Count, added, updated);
        return new SyncResult(rssItems.Count, added, updated);
    }

    private void MapInto(ParsedEpisode p, Episode e, Dictionary<string, Guest> guestCache)
    {
        e.EpisodeNumber   = p.EpisodeNumber;
        e.Title           = p.CleanedTitle;
        e.PublicationDate = p.PublicationDate;
        e.DurationSeconds = p.DurationSeconds;
        e.Description     = p.Description;
        e.SimpsonsSeason  = p.SimpsonsSeason;
        e.SimpsonsEpisode = p.SimpsonsEpisode;
        e.SimpsonsTitle   = string.IsNullOrWhiteSpace(p.SimpsonsEpisodeTitle) ? null : p.SimpsonsEpisodeTitle;
        e.SpotifyUrl      = p.SpotifyUrl;
        e.AudioUrl        = p.AudioUrl;
        e.ImageUrl        = p.ImageUrl;
        e.IsSpecial       = p.IsSpecial;

        // Reassigning the tracked collection lets EF diff the many-to-many join rows.
        e.Guests = ResolveGuests(p.Guests, guestCache);
    }

    private List<Guest> ResolveGuests(List<string> names, Dictionary<string, Guest> cache)
    {
        var guests = new List<Guest>();
        foreach (var name in names)
        {
            var trimmed = name.Trim();
            if (trimmed.Length == 0) continue;

            if (!cache.TryGetValue(trimmed, out var guest))
            {
                guest = new Guest { Name = trimmed };
                cache[trimmed] = guest;
                db.Guests.Add(guest);
            }
            if (!guests.Contains(guest))
                guests.Add(guest);
        }
        return guests;
    }
}

public record SyncResult(int FeedItems, int Added, int Updated);
