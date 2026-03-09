// Copyright (c) Ben Hughes. SPDX-License-Identifier: AGPL-3.0-or-later
namespace NowPlaying.Tests.Extensions;

using NowPlaying.Extensions;
using Xunit;

/// <summary>
/// Tests for HtmlExtensions.
/// </summary>
public class HtmlExtensionsTests
{
    /// <summary>
    /// Verifies that simple text is extracted correctly.
    /// </summary>
    [Fact]
    public void ExtractFirstLineAsAltText_WithSimpleText_ReturnsFirstLine()
    {
        var html = "Metallica - Master of Puppets";

        var result = html.ExtractFirstLineAsAltText();

        Assert.Equal("Metallica - Master of Puppets", result);
    }

    /// <summary>
    /// Verifies that #nowplaying hashtag is removed from the extracted text.
    /// </summary>
    [Fact]
    public void ExtractFirstLineAsAltText_WithNowplayingHashtag_RemovesHashtag()
    {
        var html = "Metallica - Master of Puppets #nowplaying";

        var result = html.ExtractFirstLineAsAltText();

        Assert.Equal("Metallica - Master of Puppets", result);
    }

    /// <summary>
    /// Verifies that capitalized #NowPlaying hashtag is removed from the extracted text.
    /// </summary>
    [Fact]
    public void ExtractFirstLineAsAltText_WithCapitalizedNowplaying_RemovesHashtag()
    {
        var html = "Metallica - Master of Puppets #NowPlaying";

        var result = html.ExtractFirstLineAsAltText();

        Assert.Equal("Metallica - Master of Puppets", result);
    }

    /// <summary>
    /// Verifies that HTML tags are stripped and hashtag is removed.
    /// </summary>
    [Fact]
    public void ExtractFirstLineAsAltText_WithHtmlTags_StripsTagsAndRemovesHashtag()
    {
        var html = "<p>Metallica - Master of Puppets #nowplaying</p>";

        var result = html.ExtractFirstLineAsAltText();

        Assert.Equal("Metallica - Master of Puppets", result);
    }

    /// <summary>
    /// Verifies that only the first line is returned when multiple lines are present.
    /// </summary>
    [Fact]
    public void ExtractFirstLineAsAltText_WithMultipleLines_ReturnsFirstLine()
    {
        var html = "Metallica - Master of Puppets #nowplaying\nSecond line\nThird line";

        var result = html.ExtractFirstLineAsAltText();

        Assert.Equal("Metallica - Master of Puppets", result);
    }

    /// <summary>
    /// Verifies that HTML line breaks are handled and only the first line is returned.
    /// </summary>
    [Fact]
    public void ExtractFirstLineAsAltText_WithHtmlBreaks_ReturnsFirstLine()
    {
        var html = "Metallica - Master of Puppets #nowplaying<br/>Second line";

        var result = html.ExtractFirstLineAsAltText();

        Assert.Equal("Metallica - Master of Puppets", result);
    }

    /// <summary>
    /// Verifies that HTML entities are decoded correctly.
    /// </summary>
    [Fact]
    public void ExtractFirstLineAsAltText_WithHtmlEntities_DecodesEntities()
    {
        var html = "Metallica &amp; Guns N&apos; Roses #nowplaying";

        var result = html.ExtractFirstLineAsAltText();

        Assert.Equal("Metallica & Guns N' Roses", result);
    }

    /// <summary>
    /// Verifies that a default value is returned when the input contains only a hashtag.
    /// </summary>
    [Fact]
    public void ExtractFirstLineAsAltText_WithOnlyHashtag_ReturnsDefault()
    {
        var html = "#nowplaying";

        var result = html.ExtractFirstLineAsAltText();

        Assert.Equal("Album cover", result);
    }

    /// <summary>
    /// Verifies that a default value is returned for an empty string.
    /// </summary>
    [Fact]
    public void ExtractFirstLineAsAltText_WithEmptyString_ReturnsDefault()
    {
        var html = string.Empty;

        var result = html.ExtractFirstLineAsAltText();

        Assert.Equal("Album cover", result);
    }

    /// <summary>
    /// Verifies that a default value is returned for a null input.
    /// </summary>
    [Fact]
    public void ExtractFirstLineAsAltText_WithNull_ReturnsDefault()
    {
        string? html = null;

        var result = html.ExtractFirstLineAsAltText();

        Assert.Equal("Album cover", result);
    }

    /// <summary>
    /// Verifies that extraction works correctly with complex HTML.
    /// </summary>
    [Fact]
    public void ExtractFirstLineAsAltText_WithComplexHtml_ExtractsCorrectly()
    {
        var html = "<p><a href=\"#\">Metallica</a> - Master of Puppets #nowplaying</p><p>More content</p>";

        var result = html.ExtractFirstLineAsAltText();

        Assert.Equal("Metallica - Master of Puppets", result);
    }

    /// <summary>
    /// Verifies that a default value is returned when the input contains only tags that result in no text.
    /// </summary>
    [Fact]
    public void ExtractFirstLineAsAltText_WithOnlyTags_ReturnsDefault()
    {
        var html = "<p></p><br/>";

        var result = html.ExtractFirstLineAsAltText();

        Assert.Equal("Album cover", result);
    }
}
