using PodcastApi.Parsing;
using PodcastApi.Rss;

namespace PodcastApi.Tests;

public class EpisodeParserTests
{
    private readonly EpisodeParser _parser = new();

    private ParsedEpisode Parse(string title, string description = "") =>
        _parser.ParseEpisode(new RssEpisodeItem(title, description, default, null, "", "https://audio/1.mp3", ""));

    [Fact]
    public void NumberedTitleWithGuests_ExtractsNumberTitleAndGuests()
    {
        var p = Parse("42 - El Hombre Radiactivo (con Ana, Luis y Marta) | Proletario y Parásito");

        Assert.Equal(42, p.EpisodeNumber);
        Assert.Equal("El Hombre Radiactivo", p.CleanedTitle);
        Assert.Equal(["Ana", "Luis", "Marta"], p.Guests);
        Assert.False(p.IsSpecial);
    }

    [Fact]
    public void NumberedTitleWithoutGuests_HasNoGuests()
    {
        var p = Parse("7 - Homer va a la universidad | Proletario y Parásito");

        Assert.Equal(7, p.EpisodeNumber);
        Assert.Equal("Homer va a la universidad", p.CleanedTitle);
        Assert.Empty(p.Guests);
        Assert.False(p.IsSpecial);
    }

    [Fact]
    public void NumberedSpecial_IsSpecialWithNumber()
    {
        var p = Parse("ESPECIAL #3 | Proletario y Parásito");

        Assert.Equal(3, p.EpisodeNumber);
        Assert.Equal("Especial #3", p.CleanedTitle);
        Assert.True(p.IsSpecial);
    }

    [Fact]
    public void UnrecognisedTitle_IsSpecialWithSuffixStripped()
    {
        var p = Parse("Cachitos de Navidad | Proletario y Parásito");

        Assert.Null(p.EpisodeNumber);
        Assert.Equal("Cachitos de Navidad", p.CleanedTitle);
        Assert.True(p.IsSpecial);
    }

    [Fact]
    public void NumberedTitleContainingSpecialWord_IsSpecial()
    {
        var p = Parse("12 - Bonus track | Proletario y Parásito");

        Assert.Equal(12, p.EpisodeNumber);
        Assert.True(p.IsSpecial);
    }

    [Fact]
    public void Description_StripsHtmlAndExtractsSimpsonsReference()
    {
        var p = Parse(
            "1 - Piloto | Proletario y Parásito",
            "<p>Hola &amp; bienvenidos</p>\n<p>[Episodio referencia: 4x12 - Marge contra el monorraíl]</p>");

        Assert.Equal("Hola & bienvenidos [Episodio referencia: 4x12 - Marge contra el monorraíl]", p.Description);
        Assert.Equal(4, p.SimpsonsSeason);
        Assert.Equal(12, p.SimpsonsEpisode);
        Assert.Equal("Marge contra el monorraíl", p.SimpsonsEpisodeTitle);
    }

    [Fact]
    public void DescriptionWithoutReference_LeavesSimpsonsFieldsEmpty()
    {
        var p = Parse("1 - Piloto | Proletario y Parásito", "Sin referencia");

        Assert.Null(p.SimpsonsSeason);
        Assert.Null(p.SimpsonsEpisode);
        Assert.Equal("", p.SimpsonsEpisodeTitle);
    }
}
