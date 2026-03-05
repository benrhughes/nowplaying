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
    /// <param name="registrationStore">The registration store.</param>
    /// <returns>The registration response.</returns>
    public static async Task<IResult> Register(
        HttpContext context,
        RegisterRequest request,
        IMastodonService mastodonService,
        AppConfig config,
        ILoggerFactory loggerFactory,
        IRegistrationStore registrationStore)
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

            // Persist instance in session for pre-auth flows, but keep client credentials in server-side store
            context.Session.SetString("instance", instance);
            registrationStore.Add(instance, clientId, clientSecret, config.RedirectUri);

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
    /// <param name="registrationStore">The registration store.</param>
    public static IResult Status(HttpContext context, IRegistrationStore registrationStore)
    {
        var instance = context.User.GetInstance() ?? context.Session.GetString("instance");

        var registered = !string.IsNullOrEmpty(instance) && registrationStore.Has(instance);

        return Results.Ok(new StatusResponse(
            Authenticated: context.User.Identity?.IsAuthenticated ?? false,
            Instance: instance,
            Registered: registered));
    }
}
