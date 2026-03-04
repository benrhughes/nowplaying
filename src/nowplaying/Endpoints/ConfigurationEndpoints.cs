namespace NowPlaying.Endpoints;

using NowPlaying.Extensions;
using NowPlaying.Models;
using NowPlaying.Services;

/// <summary>
/// Configuration endpoints for app setup and status.
/// </summary>
public static class ConfigurationEndpoints
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
            var logger = loggerFactory.CreateLogger(nameof(ConfigurationEndpoints));
            logger.LogWarning(ex, "App registration failed for {instance}: {message}", instance, ex.Message);
            return Results.BadRequest(new ErrorResponse($"Failed to register with instance: {ex.Message}"));
        }
        catch (Exception ex)
        {
            var logger = loggerFactory.CreateLogger(nameof(ConfigurationEndpoints));
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
}
