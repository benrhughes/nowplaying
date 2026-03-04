namespace NowPlaying.Endpoints;

using NowPlaying.Extensions;
using NowPlaying.Models;
using NowPlaying.Services;

/// <summary>
/// Endpoints for posting albums to Mastodon.
/// </summary>
public static class PostingEndpoints
{
    /// <summary>
    /// Scrapes album info from a Bandcamp URL.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="request">The scrape request.</param>
    /// <param name="bandcampService">The Bandcamp service.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <returns>The scrape response.</returns>
    public static async Task<IResult> Scrape(
        HttpContext context,
        ScrapeRequest request,
        IBandcampService bandcampService,
        ILoggerFactory loggerFactory)
    {
        var host = new Uri(request.Url).Host;
        if (!host.EndsWith(".bandcamp.com") && host != "bandcamp.com")
        {
            return Results.BadRequest(new ErrorResponse("Only Bandcamp URLs are supported"));
        }

        try
        {
            var result = await bandcampService.ScrapeAsync(request.Url);
            return Results.Ok(result);
        }
        catch (HttpRequestException ex)
        {
            var logger = loggerFactory.CreateLogger(nameof(PostingEndpoints));
            logger.LogWarning(ex, "Scrape failed for {url}: {message}", request.Url, ex.Message);
            return Results.BadRequest(new ErrorResponse($"Failed to scrape URL: {ex.Message}"));
        }
        catch (Exception ex)
        {
            var logger = loggerFactory.CreateLogger(nameof(PostingEndpoints));
            logger.LogError(ex, "Scrape failed for {url}", request.Url);
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Posts a Bandcamp album to Mastodon.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="request">The post request.</param>
    /// <param name="mastodonService">The Mastodon service.</param>
    /// <param name="imageService">The image service.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <returns>The post response.</returns>
    public static async Task<IResult> Post(
        HttpContext context,
        PostRequest request,
        IMastodonService mastodonService,
        IImageService imageService,
        ILoggerFactory loggerFactory)
    {
        var accessToken = context.Session.GetString("accessToken");
        var instance = context.Session.GetString("instance");

        if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(instance))
        {
            return Results.Unauthorized();
        }

        try
        {
            var imageData = await imageService.DownloadImageAsync(request.ImageUrl);

            var mediaId = await mastodonService.UploadMediaAsync(instance, accessToken, imageData, request.AltText);
            var (statusId, url) = await mastodonService.PostStatusAsync(instance, accessToken, request.Text, mediaId);

            return Results.Ok(new PostResponse(true, statusId, url));
        }
        catch (HttpRequestException ex)
        {
            var logger = loggerFactory.CreateLogger(nameof(PostingEndpoints));
            logger.LogWarning(ex, "Post failed due to network error: {message}", ex.Message);
            return Results.BadRequest(new ErrorResponse($"Failed to post: {ex.Message}"));
        }
        catch (Exception ex)
        {
            var logger = loggerFactory.CreateLogger(nameof(PostingEndpoints));
            logger.LogError(ex, "Post failed");
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
