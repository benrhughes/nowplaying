using System.Text.RegularExpressions;
using HtmlAgilityPack;
using NowPlaying.Models;

namespace NowPlaying.Services;

/// <summary>
/// Service for scraping Bandcamp album information.
/// </summary>
/// <param name="httpClient">The HTTP client.</param>
/// <param name="logger">The logger.</param>
public class BandcampService(HttpClient httpClient, ILogger<BandcampService> logger)
    : IBandcampService
{
    /// <inheritdoc/>
    public async Task<ScrapeResponse> ScrapeAsync(string url)
    {
        logger.LogInformation("Scraping Bandcamp URL: {Url}", url);

        try
        {
            var htmlContent = await httpClient.GetStringAsync(url);

            var doc = new HtmlDocument();
            doc.LoadHtml(htmlContent);

            var titleNode = doc.DocumentNode.SelectSingleNode("//meta[@property='og:title']");
            var imageNode = doc.DocumentNode.SelectSingleNode("//meta[@property='og:image']");
            var descriptionNode = doc.DocumentNode.SelectSingleNode("//meta[@property='og:description']");
            var titleTagNode = doc.DocumentNode.SelectSingleNode("//title");

            var title = titleNode?.GetAttributeValue("content", null) ?? titleTagNode?.InnerText ?? string.Empty;
            var image = imageNode?.GetAttributeValue("content", null);
            var description = descriptionNode?.GetAttributeValue("content", null) ?? string.Empty;

            var (artist, album) = ParseArtistAndAlbum(title);

            logger.LogDebug("Scraped info - Artist: {Artist}, Album: {Album}", artist, album);

            return new ScrapeResponse(
                Title: title,
                Artist: artist,
                Album: album,
                Image: image,
                Description: description,
                Url: url);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to scrape Bandcamp URL: {Url}", url);
            throw;
        }
    }

    private (string Artist, string Album) ParseArtistAndAlbum(string title)
    {
        if (string.IsNullOrEmpty(title))
        {
            return (string.Empty, string.Empty);
        }

        // Pattern: "Album – Artist"
        var match = Regex.Match(title, @"(.+?)\s*–\s*(.+?)(?:\s+by|on Bandcamp)?$", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            var album = match.Groups[1].Value.Trim().TrimEnd(',').Trim();
            var artist = match.Groups[2].Value.Trim();
            return (artist, album);
        }

        // Pattern: "Album by Artist"
        var byMatch = Regex.Match(title, @"(.+?)\s+by\s+(.+?)(?:\s+on Bandcamp)?$", RegexOptions.IgnoreCase);
        if (byMatch.Success)
        {
            var album = byMatch.Groups[1].Value.Trim().TrimEnd(',').Trim();
            var artist = byMatch.Groups[2].Value.Trim();
            return (artist, album);
        }

        return (string.Empty, string.Empty);
    }
}
