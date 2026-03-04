namespace NowPlaying.Extensions;

using System.Text.RegularExpressions;

/// <summary>
/// Extensions for HTML content processing.
/// </summary>
public static class HtmlExtensions
{
    /// <summary>
    /// Extracts the first line of text from HTML content and removes the #nowplaying hashtag.
    /// </summary>
    /// <param name="htmlContent">The HTML content.</param>
    /// <returns>The first line of clean text without #nowplaying.</returns>
    public static string ExtractFirstLineAsAltText(this string? htmlContent)
    {
        if (string.IsNullOrWhiteSpace(htmlContent))
        {
            return "Album cover";
        }

        // Replace block-level HTML tags and line breaks with newlines
        var normalized = Regex.Replace(htmlContent, @"</?p[^>]*>|<br\s*/?>\s*", "\n", RegexOptions.IgnoreCase);

        // Remove all other HTML tags
        var textContent = Regex.Replace(normalized, @"<[^>]+>", string.Empty);

        // Decode HTML entities
        textContent = System.Web.HttpUtility.HtmlDecode(textContent);

        // Split by line breaks and get first non-empty line
        var lines = textContent.Split(new[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0)
        {
            return "Album cover";
        }

        var firstLine = lines[0].Trim();

        // Remove #nowplaying and #NowPlaying hashtags (case-insensitive)
        firstLine = Regex.Replace(firstLine, @"#[Nn]ow[Pp]laying\s?", string.Empty).Trim();

        // If the line is empty after removing hashtag, return default
        if (string.IsNullOrEmpty(firstLine))
        {
            return "Album cover";
        }

        return firstLine;
    }
}
