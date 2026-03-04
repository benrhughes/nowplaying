namespace NowPlaying.Endpoints;

using NowPlaying.Extensions;
using NowPlaying.Models;
using NowPlaying.Services;

/// <summary>
/// Main API endpoints for registration, scraping, and posting.
/// </summary>
public static class ApiEndpoints
{
    /// <summary>
    /// Registers the application with a Mastodon instance.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="request">The registration request.</param>
    /// <param name="mastodonService">The Mastodon service.</param>
    /// <param name="config">The app configuration.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <returns>The registration response.</returns>
    public static async Task<IResult> Register(
        HttpContext context,
        RegisterRequest request,
        IMastodonService mastodonService,
        AppConfig config,
        ILoggerFactory loggerFactory)
    {
        string instance;
        try
        {
            instance = request.Instance.NormalizeInstance();
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new ErrorResponse(ex.Message));
        }

        try
        {
            var (clientId, clientSecret) = await mastodonService.RegisterAppAsync(instance, config.RedirectUri);

            context.Session.SetString("instance", instance);
            context.Session.SetString("clientId", clientId);
            context.Session.SetString("clientSecret", clientSecret);
            context.Session.SetString("redirectUri", config.RedirectUri);

            return Results.Ok(new RegistrationResponse(true, instance));
        }
        catch (HttpRequestException ex)
        {
            var logger = loggerFactory.CreateLogger(nameof(ApiEndpoints));
            logger.LogWarning(ex, "App registration failed for {instance}: {message}", instance, ex.Message);
            return Results.BadRequest(new ErrorResponse($"Failed to register with instance: {ex.Message}"));
        }
        catch (Exception ex)
        {
            var logger = loggerFactory.CreateLogger(nameof(ApiEndpoints));
            logger.LogError(ex, "App registration failed for {instance}", instance);
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Returns the current authentication status.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>The status response.</returns>
    public static IResult Status(HttpContext context)
    {
        var accessToken = context.Session.GetString("accessToken");
        var instance = context.Session.GetString("instance");
        var clientId = context.Session.GetString("clientId");

        return Results.Ok(new StatusResponse(
            Authenticated: !string.IsNullOrEmpty(accessToken),
            Instance: instance,
            Registered: !string.IsNullOrEmpty(clientId)));
    }

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
            var logger = loggerFactory.CreateLogger(nameof(ApiEndpoints));
            logger.LogWarning(ex, "Scrape failed for {url}: {message}", request.Url, ex.Message);
            return Results.BadRequest(new ErrorResponse($"Failed to scrape URL: {ex.Message}"));
        }
        catch (Exception ex)
        {
            var logger = loggerFactory.CreateLogger(nameof(ApiEndpoints));
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
            var logger = loggerFactory.CreateLogger(nameof(ApiEndpoints));
            logger.LogWarning(ex, "Post failed due to network error: {message}", ex.Message);
            return Results.BadRequest(new ErrorResponse($"Failed to post: {ex.Message}"));
        }
        catch (Exception ex)
        {
            var logger = loggerFactory.CreateLogger(nameof(ApiEndpoints));
            logger.LogError(ex, "Post failed");
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
