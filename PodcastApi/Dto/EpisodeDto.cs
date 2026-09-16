namespace PodcastApi.Dto;

public record EpisodeDto
{
    public int Id { get; init; }
    public int? EpisodeNumber { get; init; }
    public string Title { get; init; } = "";
    public string? Description { get; init; }
    public DateTime PublicationDate { get; init; }
    public string? FormattedEpisodeNumber { get; init; }
    public string? FormattedDuration { get; init; }
    public bool IsSpecial { get; init; }
    public int? SimpsonsSeason { get; init; }
    public int? SimpsonsEpisode { get; init; }
    public string? SimpsonsTitle { get; init; }
    public string? SpotifyUrl { get; init; }
    public string? AudioUrl { get; init; }
    public string? ImageUrl { get; init; }
    public string[] Guests { get; init; } = [];
}
