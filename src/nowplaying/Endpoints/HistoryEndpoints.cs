// Copyright (c) Ben Hughes. SPDX-License-Identifier: AGPL-3.0-or-later
namespace NowPlaying.Endpoints;

using NowPlaying.Extensions;
using NowPlaying.Models;
using NowPlaying.Services;

/// <summary>
/// Endpoints for reviewing listening history.
/// </summary>
/// <param name="mastodonService">The Mastodon service.</param>
/// <param name="imageService">The image service.</param>
/// <param name="compositeImageCache">The composite image cache.</param>
/// <param name="logger">The logger.</param>
public class HistoryEndpoints(
    IMastodonService mastodonService,
    IImageService imageService,
    ICompositeImageCache compositeImageCache,
    ILogger<HistoryEndpoints> logger)
{
    /// <summary>
    /// Searches for #nowplaying posts in a date range.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="request">The search request parameters.</param>
    /// <returns>A list of found posts/images.</returns>
    public async Task<IResult> Search(
        HttpContext context,
        HistorySearchRequest request)
    {
        var instance = context.User.GetInstance() ?? throw new UnauthorizedAccessException();
        var accessToken = context.User.GetAccessToken() ?? throw new UnauthorizedAccessException();

        try
        {
            var userId = await mastodonService.VerifyCredentialsAsync(instance, accessToken);
            var tag = request.Tag.TrimStart('#');
            var posts = await mastodonService.GetTaggedPostsAsync(instance, accessToken, userId, tag, request.Since!.Value, request.Until!.Value);

            var images = posts
                .Where(p => p.MediaAttachments != null && p.MediaAttachments.Count > 0)
                .Select(p => new HistorySearchResponse(
                    p.id,
                    p.CreatedAt,
                    p.MediaAttachments![0].preview_url ?? p.MediaAttachments![0].url,
                    p.content.ExtractFirstLineAsAltText()))
                .ToList();

            return Results.Ok(images);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            logger.LogWarning(ex, "Search failed with unauthorized access for {instance}", instance);
            throw;
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Search failed for {instance}: {message}", instance, ex.Message);
            return Results.BadRequest(new ErrorResponse($"Failed to search posts: {ex.Message}"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Search failed for {instance}", instance);
            return Results.BadRequest(new ErrorResponse($"An error occurred: {ex.Message}"));
        }
    }

    /// <summary>
    /// Generates a composite image from a list of URLs and caches it.
    /// </summary>
    /// <param name="request">The request containing image URLs.</param>
    /// <returns>The cache ID for the generated composite image.</returns>
    public async Task<IResult> Composite(
        CompositeRequest request)
    {
        if (request.ImageUrls == null || request.ImageUrls.Count == 0)
        {
            return Results.BadRequest(new ErrorResponse("No images provided"));
        }

        try
        {
            var imageBytes = await imageService.GenerateCompositeAsync(request.ImageUrls);
            var cacheId = compositeImageCache.Store(imageBytes);
            return Results.Ok(new CompositeResponse(cacheId, "image/jpeg"));
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Composite generation failed: {message}", ex.Message);
            return Results.BadRequest(new ErrorResponse($"Failed to download images: {ex.Message}"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Composite generation failed");
            return Results.BadRequest(new ErrorResponse($"Failed to generate composite: {ex.Message}"));
        }
    }

    /// <summary>
    /// Retrieves a cached composite image for preview/display purposes.
    /// </summary>
    /// <param name="cacheId">The cache ID of the composite image.</param>
    /// <returns>The cached composite image file.</returns>
    public IResult GetCompositePreview(string cacheId)
    {
        if (string.IsNullOrEmpty(cacheId))
        {
            return Results.BadRequest(new ErrorResponse("Cache ID is required"));
        }

        var imageData = compositeImageCache.Retrieve(cacheId);
        if (imageData == null)
        {
            logger.LogWarning("Composite cache entry not found or expired: {CacheId}", cacheId);
            return Results.NotFound(new ErrorResponse("Composite image not found or has expired"));
        }

        return Results.File(imageData, "image/jpeg");
    }

    /// <summary>
    /// Posts a composite image to Mastodon using a previously cached image.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="request">The post composite request with cache ID.</param>
    /// <returns>The status URL.</returns>
    public async Task<IResult> PostComposite(
        HttpContext context,
        PostCompositeRequest request)
    {
        var instance = context.User.GetInstance() ?? throw new UnauthorizedAccessException();
        var accessToken = context.User.GetAccessToken() ?? throw new UnauthorizedAccessException();

        try
        {
            var cacheId = request.CacheId;
            var altText = request.AltText;
            var text = request.Text;

            // Guard checks
            if (string.IsNullOrEmpty(cacheId))
            {
                return Results.BadRequest(new ErrorResponse("Cache ID is required"));
            }

            if (string.IsNullOrEmpty(text))
            {
                return Results.BadRequest(new ErrorResponse("No post text provided"));
            }

            // Retrieve image from cache
            var imageData = compositeImageCache.Retrieve(cacheId);
            if (imageData == null)
            {
                logger.LogWarning("Composite cache entry not found or expired: {CacheId}", cacheId);
                return Results.BadRequest(new ErrorResponse("Composite image not found. Please generate a new composite."));
            }

            // Upload media
            var mediaId = await mastodonService.UploadMediaAsync(instance, accessToken, imageData, altText);

            // Post status
            var (statusId, url) = await mastodonService.PostStatusAsync(instance, accessToken, text, mediaId);

            // Clear the cached image now that it's been posted
            compositeImageCache.Remove(cacheId);

            return Results.Ok(new { statusId, url });
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            logger.LogWarning(ex, "Post composite failed with unauthorized access for {instance}", instance);
            throw;
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Post composite failed for {instance}: {message}", instance, ex.Message);
            return Results.BadRequest(new ErrorResponse($"Failed to post composite: {ex.Message}"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Post composite failed for {instance}", instance);
            return Results.BadRequest(new ErrorResponse($"An error occurred: {ex.Message}"));
        }
    }
}
