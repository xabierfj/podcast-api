using PodcastApi.Rss;

namespace PodcastApi.Tests;

public class DurationParsingTests
{
    [Theory]
    [InlineData("620", 620)]
    [InlineData("10:20", 620)]
    [InlineData("01:10:20", 4220)]
    [InlineData(" 00:45 ", 45)]
    public void ParsesSecondsAndClockFormats(string input, int expected)
    {
        Assert.Equal(expected, RssFeedService.ParseDuration(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData("10:xx")]
    [InlineData("1:2:3:4")]
    public void ReturnsNullForMissingOrInvalid(string? input)
    {
        Assert.Null(RssFeedService.ParseDuration(input));
    }
}
