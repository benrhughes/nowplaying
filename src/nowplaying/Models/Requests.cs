namespace NowPlaying.Models;

using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

/// <summary>
/// Request to register an application with a Mastodon instance.
/// </summary>
public class RegisterRequest
{
    /// <summary>
    /// Gets or sets the URL of the Mastodon instance.
    /// </summary>
    [Required(ErrorMessage = "Mastodon instance URL is required")]
    public string Instance { get; set; } = string.Empty;
}

/// <summary>
/// Request to scrape album data from a Bandcamp URL.
/// </summary>
public class ScrapeRequest
{
    /// <summary>
    /// Gets or sets the URL of the Bandcamp album.
    /// </summary>
    [Required(ErrorMessage = "URL is required")]
    [Url(ErrorMessage = "Invalid URL")]
    public string Url { get; set; } = string.Empty;
}

/// <summary>
/// Request to post an album status to Mastodon.
/// </summary>
public class PostRequest
{
    /// <summary>
    /// Gets or sets the text for the status post.
    /// </summary>
    [Required(ErrorMessage = "Text is required")]
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the URL of the album image to upload.
    /// </summary>
    [Required(ErrorMessage = "Image URL is required")]
    [Url(ErrorMessage = "Invalid image URL")]
    public string ImageUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional alternative text for the image.
    /// </summary>
    public string? AltText { get; set; }
}

/// <summary>
/// Request to post multiple images to Mastodon.
/// </summary>
public class CompositeRequest
{
    /// <summary>
    /// Gets or sets the URLs of the images to upload.
    /// </summary>
    public List<string> ImageUrls { get; set; } = new ();
}

/// <summary>
/// Request model for posting a composite image via multipart/form-data.
/// </summary>
public class PostCompositeRequest
{
    /// <summary>
    /// Gets or sets the uploaded image file.
    /// </summary>
    [Required(ErrorMessage = "Image is required")]
    public IFormFile Image { get; set; } = default!;

    /// <summary>
    /// Gets or sets the alternative text for the image.
    /// </summary>
    [MaxLength(1500, ErrorMessage = "Alt text exceeds 1500 characters")]
    public string? AltText { get; set; }

    /// <summary>
    /// Gets or sets the post text to publish to Mastodon.
    /// </summary>
    [Required(ErrorMessage = "Post text is required")]
    public string Text { get; set; } = string.Empty;
}

/// <summary>
/// Request to search for history posts.
/// </summary>
public class HistorySearchRequest
{
    /// <summary>
    /// Gets or sets the start date for the search.
    /// </summary>
    [Required(ErrorMessage = "Since date is required")]
    public DateTime? Since { get; set; }

    /// <summary>
    /// Gets or sets the end date for the search.
    /// </summary>
    [Required(ErrorMessage = "Until date is required")]
    public DateTime? Until { get; set; }

    /// <summary>
    /// Gets or sets the tag to search for.
    /// </summary>
    [Required(ErrorMessage = "Tag is required")]
    public required string Tag { get; set; }
}
