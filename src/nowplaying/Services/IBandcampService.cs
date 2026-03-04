using NowPlaying.Models;

namespace NowPlaying.Services;

/// <summary>
/// Service for scraping Bandcamp album information.
/// </summary>
public interface IBandcampService
{
    /// <summary>
    /// Scrapes album metadata from a Bandcamp URL.
    /// </summary>
    /// <param name="url">The Bandcamp URL to scrape.</param>
    /// <returns>A ScrapeResponse containing album metadata.</returns>
    Task<ScrapeResponse> ScrapeAsync(string url);
}
