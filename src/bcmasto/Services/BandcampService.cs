using System.Text.RegularExpressions;
using BcMasto.Models;
using HtmlAgilityPack;

namespace BcMasto.Services;

public class BandcampService : IBandcampService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<BandcampService> _logger;

    public BandcampService(IHttpClientFactory httpClientFactory, ILogger<BandcampService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<ScrapeResponse> ScrapeAsync(string url)
    {
        var client = _httpClientFactory.CreateClient("Default");
        var htmlContent = await client.GetStringAsync(url);

        var doc = new HtmlDocument();
        doc.LoadHtml(htmlContent);

        var titleNode = doc.DocumentNode.SelectSingleNode("//meta[@property='og:title']");
        var imageNode = doc.DocumentNode.SelectSingleNode("//meta[@property='og:image']");
        var descriptionNode = doc.DocumentNode.SelectSingleNode("//meta[@property='og:description']");
        var titleTagNode = doc.DocumentNode.SelectSingleNode("//title");

        var title = titleNode?.GetAttributeValue("content", null) ?? titleTagNode?.InnerText ?? "";
        var image = imageNode?.GetAttributeValue("content", null);
        var description = descriptionNode?.GetAttributeValue("content", null) ?? "";

        var (artist, album) = ParseArtistAndAlbum(title);

        return new ScrapeResponse(
            Title: title,
            Artist: artist,
            Album: album,
            Image: image,
            Description: description,
            Url: url);
    }

    private (string Artist, string Album) ParseArtistAndAlbum(string title)
    {
        if (string.IsNullOrEmpty(title))
            return ("", "");

        // Pattern: "Album – Artist"
        var match = Regex.Match(title, @"(.+?)\s*–\s*(.+?)(?:\s+by|on Bandcamp)?$", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            return (match.Groups[2].Value.Trim(), match.Groups[1].Value.Trim());
        }

        // Pattern: "Album by Artist"
        var byMatch = Regex.Match(title, @"(.+?)\s+by\s+(.+?)(?:\s+on Bandcamp)?$", RegexOptions.IgnoreCase);
        if (byMatch.Success)
        {
            return (byMatch.Groups[2].Value.Trim(), byMatch.Groups[1].Value.Trim());
        }

        return ("", "");
    }
}
