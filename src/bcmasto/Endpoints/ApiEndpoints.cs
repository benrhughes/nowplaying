using BcMasto.Extensions;
using BcMasto.Models;
using BcMasto.Services;

namespace BcMasto.Endpoints;

public static class ApiEndpoints
{
    public static async Task<IResult> Register(
        HttpContext context,
        RegisterRequest request,
        IMastodonService mastodonService,
        AppConfig config)
    {
        if (string.IsNullOrEmpty(request.Instance))
        {
            return Results.BadRequest(new ErrorResponse("Mastodon instance URL is required"));
        }

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
            Console.WriteLine($"App registration failed for {instance}: {ex.Message}");
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
        IBandcampService bandcampService)
    {
        if (string.IsNullOrEmpty(request.Url))
        {
            return Results.BadRequest(new ErrorResponse("URL is required"));
        }

        Uri parsedUrl;
        try
        {
            parsedUrl = new Uri(request.Url);
        }
        catch
        {
            return Results.BadRequest(new ErrorResponse("Invalid URL"));
        }

        var host = parsedUrl.Host;
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
            Console.WriteLine($"Scrape failed for {request.Url}: {ex.Message}");
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    public static async Task<IResult> Post(
        HttpContext context,
        PostRequest request,
        IMastodonService mastodonService,
        IHttpClientFactory httpClientFactory)
    {
        var accessToken = context.Session.GetString("accessToken");
        var instance = context.Session.GetString("instance");

        if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(instance))
        {
            return Results.Unauthorized();
        }

        if (string.IsNullOrEmpty(request.Text) || string.IsNullOrEmpty(request.ImageUrl))
        {
            return Results.BadRequest(new ErrorResponse("Text and image are required"));
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
            Console.WriteLine($"Post failed: {ex.Message}");
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
