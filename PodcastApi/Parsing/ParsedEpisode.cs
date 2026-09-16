namespace PodcastApi.Parsing;

public class ParsedEpisode
{
    public int? EpisodeNumber { get; set; }
    public string OriginalTitle { get; set; } = "";
    public string CleanedTitle { get; set; } = "";
    public List<string> Guests { get; set; } = new();
    public bool IsSpecial { get; set; }

    public int? SimpsonsSeason { get; set; }
    public int? SimpsonsEpisode { get; set; }
    public string SimpsonsEpisodeTitle { get; set; } = "";

    public string Description { get; set; } = "";
    public DateTime PublicationDate { get; set; }
    public int? DurationSeconds { get; set; }
    public string SpotifyUrl { get; set; } = "";
    public string AudioUrl { get; set; } = "";
    public string ImageUrl { get; set; } = "";
}