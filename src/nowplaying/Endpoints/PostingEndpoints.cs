// Copyright (c) Ben Hughes. SPDX-License-Identifier: AGPL-3.0-or-later
namespace NowPlaying.Endpoints;

using NowPlaying.Extensions;
using NowPlaying.Models;
using NowPlaying.Services;

/// <summary>
/// Endpoints for posting albums to Mastodon.
/// </summary>
/// <param name="bandcampService">The Bandcamp service.</param>
/// <param name="mastodonService">The Mastodon service.</param>
/// <param name="imageService">The image service.</param>
/// <param name="logger">The logger.</param>
public class PostingEndpoints(
    IBandcampService bandcampService,
    IMastodonService mastodonService,
    IImageService imageService,
    ILogger<PostingEndpoints> logger)
{
    /// <summary>
    /// Scrapes album info from a Bandcamp URL.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="request">The scrape request.</param>
    /// <returns>The scrape response.</returns>
    public async Task<IResult> Scrape(
        HttpContext context,
        ScrapeRequest request)
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
            logger.LogWarning(ex, "Scrape failed for {url}: {message}", request.Url, ex.Message);
            return Results.BadRequest(new ErrorResponse($"Failed to scrape URL: {ex.Message}"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Scrape failed for {url}", request.Url);
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Posts a Bandcamp album to Mastodon.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="request">The post request.</param>
    /// <returns>The post response.</returns>
    public async Task<IResult> Post(
        HttpContext context,
        PostRequest request)
    {
        var instance = context.User.GetInstance() ?? throw new UnauthorizedAccessException();
        var accessToken = context.User.GetAccessToken() ?? throw new UnauthorizedAccessException();

        try
        {
            var imageData = await imageService.DownloadImageAsync(request.ImageUrl);

            var mediaId = await mastodonService.UploadMediaAsync(instance, accessToken, imageData, request.AltText);
            var (statusId, url) = await mastodonService.PostStatusAsync(instance, accessToken, request.Text, mediaId);

            return Results.Ok(new PostResponse(true, statusId, url));
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            logger.LogWarning(ex, "Post failed with unauthorized access");
            throw;
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Post failed due to network error: {message}", ex.Message);
            return Results.BadRequest(new ErrorResponse($"Failed to post: {ex.Message}"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Post failed");
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
