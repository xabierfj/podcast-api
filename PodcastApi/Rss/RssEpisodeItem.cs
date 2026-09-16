namespace PodcastApi.Rss;

public record RssEpisodeItem(
    string Title,
    string DescriptionText,
    DateTime PublicationDate,
    int? DurationSeconds,
    string SpotifyUrl,
    string AudioUrl,
    string ImageUrl);