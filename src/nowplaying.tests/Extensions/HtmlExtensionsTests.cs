namespace NowPlaying.Tests.Extensions;

using NowPlaying.Extensions;
using Xunit;

/// <summary>
/// Tests for HtmlExtensions.
/// </summary>
public class HtmlExtensionsTests
{
    [Fact]
    public void ExtractFirstLineAsAltText_WithSimpleText_ReturnsFirstLine()
    {
        var html = "Metallica - Master of Puppets";

        var result = html.ExtractFirstLineAsAltText();

        Assert.Equal("Metallica - Master of Puppets", result);
    }

    [Fact]
    public void ExtractFirstLineAsAltText_WithNowplayingHashtag_RemovesHashtag()
    {
        var html = "Metallica - Master of Puppets #nowplaying";

        var result = html.ExtractFirstLineAsAltText();

        Assert.Equal("Metallica - Master of Puppets", result);
    }

    [Fact]
    public void ExtractFirstLineAsAltText_WithCapitalizedNowplaying_RemovesHashtag()
    {
        var html = "Metallica - Master of Puppets #NowPlaying";

        var result = html.ExtractFirstLineAsAltText();

        Assert.Equal("Metallica - Master of Puppets", result);
    }

    [Fact]
    public void ExtractFirstLineAsAltText_WithHtmlTags_StripsTagsAndRemovesHashtag()
    {
        var html = "<p>Metallica - Master of Puppets #nowplaying</p>";

        var result = html.ExtractFirstLineAsAltText();

        Assert.Equal("Metallica - Master of Puppets", result);
    }

    [Fact]
    public void ExtractFirstLineAsAltText_WithMultipleLines_ReturnsFirstLine()
    {
        var html = "Metallica - Master of Puppets #nowplaying\nSecond line\nThird line";

        var result = html.ExtractFirstLineAsAltText();

        Assert.Equal("Metallica - Master of Puppets", result);
    }

    [Fact]
    public void ExtractFirstLineAsAltText_WithHtmlBreaks_ReturnsFirstLine()
    {
        var html = "Metallica - Master of Puppets #nowplaying<br/>Second line";

        var result = html.ExtractFirstLineAsAltText();

        Assert.Equal("Metallica - Master of Puppets", result);
    }

    [Fact]
    public void ExtractFirstLineAsAltText_WithHtmlEntities_DecodesEntities()
    {
        var html = "Metallica &amp; Guns N&apos; Roses #nowplaying";

        var result = html.ExtractFirstLineAsAltText();

        Assert.Equal("Metallica & Guns N' Roses", result);
    }

    [Fact]
    public void ExtractFirstLineAsAltText_WithOnlyHashtag_ReturnsDefault()
    {
        var html = "#nowplaying";

        var result = html.ExtractFirstLineAsAltText();

        Assert.Equal("Album cover", result);
    }

    [Fact]
    public void ExtractFirstLineAsAltText_WithEmptyString_ReturnsDefault()
    {
        var html = string.Empty;

        var result = html.ExtractFirstLineAsAltText();

        Assert.Equal("Album cover", result);
    }

    [Fact]
    public void ExtractFirstLineAsAltText_WithNull_ReturnsDefault()
    {
        string? html = null;

        var result = html.ExtractFirstLineAsAltText();

        Assert.Equal("Album cover", result);
    }

    [Fact]
    public void ExtractFirstLineAsAltText_WithComplexHtml_ExtractsCorrectly()
    {
        var html = "<p><a href=\"#\">Metallica</a> - Master of Puppets #nowplaying</p><p>More content</p>";

        var result = html.ExtractFirstLineAsAltText();

        Assert.Equal("Metallica - Master of Puppets", result);
    }
}
