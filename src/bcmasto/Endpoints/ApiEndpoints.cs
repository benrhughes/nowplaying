namespace BcMasto.Endpoints;

using BcMasto.Extensions;
using BcMasto.Models;
using BcMasto.Services;

public static class ApiEndpoints
{
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
            var uri = new Uri(request.Instance);
            instance = uri.GetLeftPart(UriPartial.Authority);
        }
        catch
        {
            return Results.BadRequest(new ErrorResponse("Invalid instance URL"));
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
        catch (Exception ex)
        {
            var logger = loggerFactory.CreateLogger(nameof(ApiEndpoints));
            logger.LogError(ex, "App registration failed for {instance}", instance);
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

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
        catch (Exception ex)
        {
            var logger = loggerFactory.CreateLogger(nameof(ApiEndpoints));
            logger.LogError(ex, "Scrape failed for {url}", request.Url);
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    public static async Task<IResult> Post(
        HttpContext context,
        PostRequest request,
        IMastodonService mastodonService,
        IHttpClientFactory httpClientFactory,
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
            var client = httpClientFactory.CreateClient("Default");
            var imageData = await client.GetByteArrayAsync(request.ImageUrl);

            var mediaId = await mastodonService.UploadMediaAsync(instance, accessToken, imageData, request.AltText);
            var (statusId, url) = await mastodonService.PostStatusAsync(instance, accessToken, request.Text, mediaId);

            return Results.Ok(new PostResponse(true, statusId, url));
        }
        catch (Exception ex)
        {
            var logger = loggerFactory.CreateLogger(nameof(ApiEndpoints));
            logger.LogError(ex, "Post failed");
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
