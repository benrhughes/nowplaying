namespace BcMasto.Models;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Request to register an application with a Mastodon instance.
/// </summary>
/// <param name="Instance">The URL of the Mastodon instance.</param>
public record RegisterRequest([property: Required(ErrorMessage = "Mastodon instance URL is required")] string Instance);

/// <summary>
/// Request to scrape album data from a Bandcamp URL.
/// </summary>
/// <param name="Url">The URL of the Bandcamp album.</param>
public record ScrapeRequest(
    [property: Required(ErrorMessage = "URL is required")]
    [property: Url(ErrorMessage = "Invalid URL")]
    string Url);

/// <summary>
/// Request to post an album status to Mastodon.
/// </summary>
/// <param name="Text">The text for the status post.</param>
/// <param name="ImageUrl">The URL of the album image to upload.</param>
/// <param name="AltText">Optional alternative text for the image.</param>
public record PostRequest(
    [property: Required(ErrorMessage = "Text is required")] string Text,
    [property: Required(ErrorMessage = "Image URL is required")]
    [property: Url(ErrorMessage = "Invalid image URL")] string ImageUrl,
    string? AltText = null);
