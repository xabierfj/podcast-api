using System.ComponentModel.DataAnnotations.Schema;

namespace PodcastApi.Domain;

public class Episode
{
    public int Id { get; set; }
    public int? EpisodeNumber { get; set; }
    public string Title { get; set; } = "";
    public DateTime PublicationDate { get; set; }
    public int? DurationSeconds { get; set; }
    public string? Description { get; set; }
    public int? SimpsonsSeason { get; set; }
    public int? SimpsonsEpisode { get; set; }
    public string? SimpsonsTitle { get; set; }
    public string? SpotifyUrl { get; set; }
    public string? AudioUrl { get; set; }      // unique dedup key
    public string? ImageUrl { get; set; }
    public bool IsSpecial { get; set; }

    public List<Guest> Guests { get; set; } = new();

    // computed, not stored
    [NotMapped] public string FormattedEpisodeNumber =>
        EpisodeNumber.HasValue ? $"#{EpisodeNumber}" : "ESPECIAL";

    [NotMapped] public string? FormattedDuration =>
        DurationSeconds.HasValue
            ? TimeSpan.FromSeconds(DurationSeconds.Value).ToString(@"hh\:mm\:ss")
            : null;
}