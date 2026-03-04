namespace NowPlaying.Endpoints;

using NowPlaying.Models;
using NowPlaying.Services;

/// <summary>
/// Authentication endpoints for OAuth.
/// </summary>
public static class AuthEndpoints
{
    /// <summary>
    /// Initiates the OAuth login process.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="instance">The optional instance URL.</param>
    /// <param name="clientId">The optional client ID.</param>
    /// <returns>A redirect to the OAuth authorize URL.</returns>
    public static IResult Login(HttpContext context, string? instance, string? clientId)
    {
        instance ??= context.Session.GetString("instance");
        clientId ??= context.Session.GetString("clientId");

        if (string.IsNullOrEmpty(instance) || string.IsNullOrEmpty(clientId))
        {
            return Results.BadRequest(new ErrorResponse("Instance not configured. Please select an instance first."));
        }

        // Ensure no trailing slashes in instance URL
        instance = instance.TrimEnd('/');

        var redirectUri = context.Session.GetString("redirectUri");
        if (string.IsNullOrEmpty(redirectUri))
        {
            return Results.BadRequest(new ErrorResponse("Redirect URI not found in session. Please register your instance first."));
        }

        var authUrl = $"{instance}/oauth/authorize?" +
                      $"client_id={Uri.EscapeDataString(clientId)}" +
                      $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                      $"&response_type=code" +
                      $"&scope={Uri.EscapeDataString(AppConfig.OAuthScopes)}";

        return Results.Redirect(authUrl);
    }

    /// <summary>
    /// Handles the OAuth callback.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="code">The authorization code.</param>
    /// <param name="mastodonService">The Mastodon service.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <returns>A redirect to the homepage.</returns>
    public static async Task<IResult> Callback(
        HttpContext context,
        string? code,
        IMastodonService mastodonService,
        ILoggerFactory loggerFactory)
    {
        if (string.IsNullOrEmpty(code))
        {
            return Results.BadRequest(new ErrorResponse("No authorization code provided"));
        }

        var instance = context.Session.GetString("instance");
        var clientId = context.Session.GetString("clientId");
        var clientSecret = context.Session.GetString("clientSecret");

        if (string.IsNullOrEmpty(instance) || string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
        {
            return Results.BadRequest(new ErrorResponse("Session invalid. Please start the login process again."));
        }

        try
        {
            var redirectUri = context.Session.GetString("redirectUri") ?? "http://localhost:4444/auth/callback";
            var accessToken = await mastodonService.GetAccessTokenAsync(instance, clientId, clientSecret, code, redirectUri);
            context.Session.SetString("accessToken", accessToken?.Trim() ?? string.Empty);

            return Results.Redirect("/");
        }
        catch (HttpRequestException ex)
        {
            var logger = loggerFactory.CreateLogger(nameof(AuthEndpoints));
            logger.LogWarning(ex, "OAuth callback failed: {message}", ex.Message);
            return Results.BadRequest(new ErrorResponse($"OAuth failed: {ex.Message}"));
        }
        catch (Exception ex)
        {
            var logger = loggerFactory.CreateLogger(nameof(AuthEndpoints));
            logger.LogError(ex, "OAuth callback failed");
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Logs the user out by clearing the session.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A redirect to the homepage.</returns>
    public static IResult Logout(HttpContext context)
    {
        context.Session.Clear();
        return Results.Redirect("/");
    }
}
