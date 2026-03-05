namespace NowPlaying.Endpoints;

using NowPlaying.Extensions;
using NowPlaying.Models;
using NowPlaying.Services;

/// <summary>
/// Endpoints for reviewing listening history.
/// </summary>
public static class HistoryEndpoints
{
    /// <summary>
    /// Searches for #nowplaying posts in a date range.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="since">Start date.</param>
    /// <param name="until">End date.</param>
    /// <param name="mastodonService">The Mastodon service.</param>
    /// <returns>A list of found posts/images.</returns>
    public static async Task<IResult> Search(
        HttpContext context,
        DateTime since,
        DateTime until,
        IMastodonService mastodonService)
    {
        var instance = context.User.GetInstance();
        var accessToken = context.User.GetAccessToken();

        if (string.IsNullOrEmpty(instance) || string.IsNullOrEmpty(accessToken))
        {
            return Results.Unauthorized();
        }

        try
        {
            var userId = await mastodonService.VerifyCredentialsAsync(instance, accessToken);
            var posts = await mastodonService.GetTaggedPostsAsync(instance, accessToken, userId, "nowplaying", since, until);

            var images = posts
                .Where(p => p.MediaAttachments != null && p.MediaAttachments.Count > 0)
                .Select(p => new
                {
                    PostId = p.id,
                    CreatedAt = p.CreatedAt,
                    ImageUrl = p.MediaAttachments![0].preview_url ?? p.MediaAttachments![0].url,
                    AltText = p.content.ExtractFirstLineAsAltText(),
                })
                .ToList();

            return Results.Ok(images);
        }
        catch (HttpRequestException ex)
        {
            return Results.BadRequest(new ErrorResponse($"Failed to fetch posts: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new ErrorResponse($"An error occurred: {ex.Message}"));
        }
    }

    /// <summary>
    /// Generates a composite image from a list of URLs.
    /// </summary>
    /// <param name="request">The request containing image URLs.</param>
    /// <param name="imageService">The image service.</param>
    /// <returns>The generated JPEG image.</returns>
    public static async Task<IResult> Composite(
        CompositeRequest request,
        IImageService imageService)
    {
        if (request.ImageUrls == null || request.ImageUrls.Count == 0)
        {
            return Results.BadRequest(new ErrorResponse("No images provided"));
        }

        try
        {
            var imageBytes = await imageService.GenerateCompositeAsync(request.ImageUrls);
            return Results.File(imageBytes, "image/jpeg");
        }
        catch (HttpRequestException ex)
        {
            return Results.BadRequest(new ErrorResponse($"Failed to download images: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new ErrorResponse($"Failed to generate composite: {ex.Message}"));
        }
    }

    /// <summary>
    /// Posts a composite image to Mastodon.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="request">The post composite request bound from the form.</param>
    /// <param name="mastodonService">The Mastodon service.</param>
    /// <returns>The status URL.</returns>
    public static async Task<IResult> PostComposite(
        HttpContext context,
        [Microsoft.AspNetCore.Mvc.FromForm] PostCompositeRequest request,
        IMastodonService mastodonService)
    {
        var instance = context.User.GetInstance();
        var accessToken = context.User.GetAccessToken();

        if (string.IsNullOrEmpty(instance) || string.IsNullOrEmpty(accessToken))
        {
            return Results.Unauthorized();
        }

        try
        {
            var image = request.Image;
            var altText = request.AltText;
            var text = request.Text;

            // Guard checks (also covered by ValidationFilter when used in the pipeline)
            if (image == null || image.Length == 0)
            {
                return Results.BadRequest(new ErrorResponse("No image provided"));
            }

            if (string.IsNullOrEmpty(text))
            {
                return Results.BadRequest(new ErrorResponse("No post text provided"));
            }

            // Read the file stream now
            using (var stream = image.OpenReadStream())
            {
                using (var memoryStream = new MemoryStream())
                {
                    await stream.CopyToAsync(memoryStream);
                    var imageData = memoryStream.ToArray();

                    // Upload media
                    var mediaId = await mastodonService.UploadMediaAsync(instance, accessToken, imageData, altText);

                    // Post status
                    var (statusId, url) = await mastodonService.PostStatusAsync(instance, accessToken, text, mediaId);

                    return Results.Ok(new { statusId, url });
                }
            }
        }
        catch (HttpRequestException ex)
        {
            return Results.BadRequest(new ErrorResponse($"Failed to post composite: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new ErrorResponse($"An error occurred: {ex.Message}"));
        }
    }
}
