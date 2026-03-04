namespace NowPlaying.Endpoints;

using NowPlaying.Extensions;
using NowPlaying.Models;
using NowPlaying.Services;

/// <summary>
/// Endpoints for reviewing listening history.
/// </summary>
public static class ReviewEndpoints
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
        var instance = context.Session.GetString("instance");
        var accessToken = context.Session.GetString("accessToken");

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
        catch (Exception ex)
        {
            return Results.BadRequest(new ErrorResponse(ex.Message));
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
        catch (Exception ex)
        {
            return Results.BadRequest(new ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Posts a composite image to Mastodon.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="mastodonService">The Mastodon service.</param>
    /// <returns>The status URL.</returns>
    public static async Task<IResult> PostComposite(
        HttpContext context,
        IMastodonService mastodonService)
    {
        var instance = context.Session.GetString("instance");
        var accessToken = context.Session.GetString("accessToken");

        if (string.IsNullOrEmpty(instance) || string.IsNullOrEmpty(accessToken))
        {
            return Results.Unauthorized();
        }

        try
        {
            var form = await context.Request.ReadFormAsync();
            var imageFile = form.Files.GetFile("image");
            var altText = form["altText"].ToString();
            var text = form["text"].ToString();

            if (imageFile == null || imageFile.Length == 0)
            {
                return Results.BadRequest(new ErrorResponse("No image provided"));
            }

            if (string.IsNullOrEmpty(text))
            {
                return Results.BadRequest(new ErrorResponse("No post text provided"));
            }

            // Read image file to byte array
            using (var stream = imageFile.OpenReadStream())
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
        catch (Exception ex)
        {
            return Results.BadRequest(new ErrorResponse(ex.Message));
        }
    }
}
