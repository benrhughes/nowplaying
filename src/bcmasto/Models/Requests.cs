namespace BcMasto.Models;

using System.ComponentModel.DataAnnotations;

public record RegisterRequest([property: Required(ErrorMessage = "Mastodon instance URL is required")] string Instance);

public record ScrapeRequest(
    [property: Required(ErrorMessage = "URL is required")]
    [property: Url(ErrorMessage = "Invalid URL")]
    string Url);

public record PostRequest(
    [property: Required(ErrorMessage = "Text is required")] string Text,
    [property: Required(ErrorMessage = "Image URL is required")]
    [property: Url(ErrorMessage = "Invalid image URL")] string ImageUrl,
    string? AltText = null);
