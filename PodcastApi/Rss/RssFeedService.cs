using System.Globalization;
using System.Xml.Linq;
namespace PodcastApi.Rss;

public class RssFeedService(HttpClient http, IConfiguration cfg)
{
    private static readonly XNamespace Itunes = "http://www.itunes.com/dtds/podcast-1.0.dtd";

    public async Task<List<RssEpisodeItem>> GetEpisodesAsync(CancellationToken ct = default)
    {
        var url = cfg["Rss:FeedUrl"]
                  ?? throw new InvalidOperationException("Rss:FeedUrl is not configured.");

        await using var stream = await http.GetStreamAsync(url, ct);
        var doc = await XDocument.LoadAsync(stream, LoadOptions.None, ct);

        return doc.Descendants("item")
            .Select(item => new RssEpisodeItem(
                Title:           (string?)item.Element("title") ?? "",
                DescriptionText: (string?)item.Element("description") ?? "",
                PublicationDate: ParseDate((string?)item.Element("pubDate")),
                DurationSeconds: ParseDuration((string?)item.Element(Itunes + "duration")),
                SpotifyUrl:      (string?)item.Element("link") ?? "",
                AudioUrl:        (string?)item.Element("enclosure")?.Attribute("url") ?? "",
                ImageUrl:        (string?)item.Element(Itunes + "image")?.Attribute("href") ?? ""))
            .ToList();
    }

    private static DateTime ParseDate(string? value) =>
        DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var date)
            ? date.UtcDateTime
            : default;

    /// <summary>
    /// Accepts iTunes duration as either total seconds ("620")
    /// or a clock string ("mm:ss" / "hh:mm:ss"). Returns null when absent/invalid.
    /// </summary>
    public static int? ParseDuration(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        value = value.Trim();

        if (int.TryParse(value, out var totalSeconds))
            return totalSeconds;

        var parts = value.Split(':');
        if (parts.Any(p => !int.TryParse(p, out _)))
            return null;

        var nums = parts.Select(int.Parse).ToArray();
        return nums.Length switch
        {
            3 => nums[0] * 3600 + nums[1] * 60 + nums[2],
            2 => nums[0] * 60 + nums[1],
            _ => null
        };
    }
}