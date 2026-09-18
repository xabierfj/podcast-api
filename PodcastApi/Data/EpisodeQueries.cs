using PodcastApi.Domain;

namespace PodcastApi.Data;

public static class EpisodeQueries
{
    public static IOrderedQueryable<Episode> NewestFirst(this IQueryable<Episode> episodes) =>
        episodes
            .OrderBy(e => e.EpisodeNumber == null)
            .ThenByDescending(e => e.EpisodeNumber);
}
