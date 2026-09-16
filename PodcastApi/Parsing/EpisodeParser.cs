using System.Net;
using System.Text.RegularExpressions;
using PodcastApi.Rss;

namespace PodcastApi.Parsing;

public class EpisodeParser
{
    private const string PodcastSuffix = @"\|\s*Proletario y Parásito";

    private static readonly Regex TitleWithGuestRegex = new(
        @$"^(?<number>\d+)\s*-\s*(?<title>.+?)\s*\(con\s+(?<guests>.+?)\)\s*{PodcastSuffix}",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex TitleWithoutGuestRegex = new(
        @$"^(?<number>\d+)\s*-\s*(?<title>.+?)\s*{PodcastSuffix}",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex SpecialWithNumberRegex = new(
        @$"^ESPECIAL\s*#(?<number>\d+)\s*{PodcastSuffix}",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex ReferenceRegex = new(
        @"\[Episodio referencia:\s*(?<season>\d+)x(?<episode>\d+)\s*-\s*(?<title>.+?)\]",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex HtmlTagRegex = new(
        "<.*?>", RegexOptions.Compiled | RegexOptions.Singleline);

    private static readonly Regex WhitespaceRegex = new(
        @"\s+", RegexOptions.Compiled);

    private static readonly string[] SpecialWords =
        ["cachitos", "noticiario", "especial", "extra", "bonus"];

    public ParsedEpisode ParseEpisode(RssEpisodeItem rssEpisode)
    {
        var cleanDescription = StripHtml(rssEpisode.DescriptionText);

        var result = new ParsedEpisode
        {
            OriginalTitle = rssEpisode.Title,
            Description = cleanDescription,
            PublicationDate = rssEpisode.PublicationDate,
            DurationSeconds = rssEpisode.DurationSeconds,
            SpotifyUrl = rssEpisode.SpotifyUrl,
            AudioUrl = rssEpisode.AudioUrl,
            ImageUrl = rssEpisode.ImageUrl
        };

        ParseTitle(rssEpisode.Title, result);
        result.IsSpecial = result.IsSpecial || IsSpecialEpisode(rssEpisode.Title);
        ParseDescription(cleanDescription, result);
        return result;
    }

    /// <summary>
    /// Removes HTML tags, decodes entities (&amp;, &lt;, ...) and collapses
    /// runs of whitespace into a single space. Returns "" for null/blank input.
    /// </summary>
    private static string StripHtml(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return "";

        var withoutTags = HtmlTagRegex.Replace(html, " ");
        var decoded = WebUtility.HtmlDecode(withoutTags);
        return WhitespaceRegex.Replace(decoded, " ").Trim();
    }

    private void ParseDescription(string description, ParsedEpisode result)
    {
        if (string.IsNullOrWhiteSpace(description))
            return;

        var match = ReferenceRegex.Match(description);
        if (match.Success)
        {
            result.SimpsonsSeason = int.Parse(match.Groups["season"].Value);
            result.SimpsonsEpisode = int.Parse(match.Groups["episode"].Value);
            result.SimpsonsEpisodeTitle = match.Groups["title"].Value.Trim();
        }
    }

    private bool IsSpecialEpisode(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return false;
        return SpecialWords.Any(word => title.Contains(word, StringComparison.OrdinalIgnoreCase));
    }

    private void ParseTitle(string title, ParsedEpisode result)
    {
        if (string.IsNullOrWhiteSpace(title))
            return;
        var trimmed = title.Trim();

        if (TrySpecialParse(trimmed, result)) return;
        if (TryParseWithGuest(trimmed, result)) return;
        if (TryParseWithoutGuest(trimmed, result)) return;

        result.IsSpecial = true;
        result.CleanedTitle = trimmed
            .Replace("| Proletario y Parásito", "", StringComparison.OrdinalIgnoreCase)
            .Trim();
    }

    private static int? ParseEpisodeNumber(string value) =>
        int.TryParse(value, out var number) ? number : null;

    private static List<string> ParseGuests(string guestsText)
    {
        if (string.IsNullOrWhiteSpace(guestsText))
            return [];

        return guestsText
            .Split(',')
            .SelectMany(part => part.Split([" y "], StringSplitOptions.None))
            .Select(name => name.Trim())
            .Where(name => !string.IsNullOrEmpty(name))
            .ToList();
    }

    private static bool TryParseWithoutGuest(string title, ParsedEpisode result)
    {
        var match = TitleWithoutGuestRegex.Match(title);
        if (!match.Success) return false;
        result.EpisodeNumber = ParseEpisodeNumber(match.Groups["number"].Value);
        result.CleanedTitle = match.Groups["title"].Value.Trim();
        return true;
    }

    private static bool TryParseWithGuest(string title, ParsedEpisode result)
    {
        var match = TitleWithGuestRegex.Match(title);
        if (!match.Success) return false;
        result.EpisodeNumber = ParseEpisodeNumber(match.Groups["number"].Value);
        result.CleanedTitle = match.Groups["title"].Value.Trim();
        result.Guests = ParseGuests(match.Groups["guests"].Value);
        return true;
    }

    private static bool TrySpecialParse(string title, ParsedEpisode result)
    {
        var match = SpecialWithNumberRegex.Match(title);
        if (!match.Success) return false;
        result.EpisodeNumber = ParseEpisodeNumber(match.Groups["number"].Value);
        result.CleanedTitle = $"Especial #{match.Groups["number"].Value}";
        result.IsSpecial = true;
        return true;
    }
}
