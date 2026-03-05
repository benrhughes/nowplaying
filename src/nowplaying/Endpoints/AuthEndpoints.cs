namespace NowPlaying.Endpoints;

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using NowPlaying.Extensions;
using NowPlaying.Models;
using NowPlaying.Services;

/// <summary>
/// Authentication endpoints for OAuth.
/// </summary>
public static class AuthenticationEndpoints
{
    /// <summary>
    /// Initiates the OAuth login process.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="instance">The optional instance URL.</param>
    /// <param name="registrationStore">The registration store.</param>
    /// <returns>A redirect to the OAuth authorize URL.</returns>
    public static IResult Login(HttpContext context, string? instance, IRegistrationStore registrationStore)
    {
        instance ??= context.Session.GetString("instance");

        if (string.IsNullOrEmpty(instance))
        {
            return Results.BadRequest(new ErrorResponse("Instance not configured. Please select an instance first."));
        }

        // Ensure no trailing slashes in instance URL
        instance = instance.TrimEnd('/');

        if (!registrationStore.TryGet(instance, out var info) || info == null)
        {
            return Results.BadRequest(new ErrorResponse("Instance not registered. Please register your instance first."));
        }

        var clientId = info.ClientId;
        var redirectUri = info.RedirectUri;

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
    /// <param name="registrationStore">The registration store.</param>
    /// <returns>A redirect to the homepage.</returns>
    public static async Task<IResult> Callback(
        HttpContext context,
        string? code,
        IMastodonService mastodonService,
        ILoggerFactory loggerFactory,
        IRegistrationStore registrationStore)
    {
        if (string.IsNullOrEmpty(code))
        {
            return Results.BadRequest(new ErrorResponse("No authorization code provided"));
        }

        var instance = context.Session.GetString("instance");

        if (string.IsNullOrEmpty(instance))
        {
            return Results.BadRequest(new ErrorResponse("Session invalid. Please start the login process again."));
        }

        if (!registrationStore.TryGet(instance, out var reg) || reg == null)
        {
            return Results.BadRequest(new ErrorResponse("Registration info missing. Please register the instance again."));
        }

        var clientId = reg.ClientId;
        var clientSecret = reg.ClientSecret;
        var redirectUri = reg.RedirectUri ?? "http://localhost:4444/auth/callback";

        try
        {
            var accessToken = await mastodonService.GetAccessTokenAsync(instance, clientId, clientSecret, code, redirectUri);

            // Create claims for the authenticated user
            var claims = ClaimsExtensions.CreateAuthenticationClaims(instance, accessToken?.Trim() ?? string.Empty);
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            // Sign in the user with cookie authentication
            await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(24)
            });

            return Results.Redirect("/");
        }
        catch (HttpRequestException ex)
        {
            var logger = loggerFactory.CreateLogger(nameof(AuthenticationEndpoints));
            logger.LogWarning(ex, "OAuth callback failed: {message}", ex.Message);
            return Results.BadRequest(new ErrorResponse($"OAuth failed: {ex.Message}"));
        }
        catch (Exception ex)
        {
            var logger = loggerFactory.CreateLogger(nameof(AuthenticationEndpoints));
            logger.LogError(ex, "OAuth callback failed");
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Logs the user out by clearing authentication and session.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A redirect to the homepage.</returns>
    public static async Task<IResult> Logout(HttpContext context)
    {
        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        context.Session.Clear();
        return Results.Redirect("/");
    }
}
