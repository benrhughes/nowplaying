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
/// <param name="mastodonService">The Mastodon service.</param>
/// <param name="logger">The logger.</param>
/// <param name="registrationStore">The registration store.</param>
/// <param name="config">The app configuration.</param>
public class AuthenticationEndpoints(
    IMastodonService mastodonService,
    ILogger<AuthenticationEndpoints> logger,
    IRegistrationStore registrationStore,
    AppConfig config)
{
    /// <summary>
    /// Initiates the OAuth login process.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="instance">The optional instance URL.</param>
    /// <returns>A redirect to the OAuth authorize URL.</returns>
    public IResult Login(HttpContext context, string? instance)
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
        var state = Guid.NewGuid().ToString("N");
        context.Session.SetString("oauth_state", state);

        var authUrl = $"{instance}/oauth/authorize?" +
                      $"client_id={Uri.EscapeDataString(clientId)}" +
                      $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                      $"&response_type=code" +
                      $"&scope={Uri.EscapeDataString(AppConfig.OAuthScopes)}" +
                      $"&state={state}";

        return Results.Redirect(authUrl);
    }

    /// <summary>
    /// Handles the OAuth callback.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="code">The authorization code.</param>
    /// <param name="state">The state parameter for CSRF protection.</param>
    /// <returns>A redirect to the homepage.</returns>
    public async Task<IResult> Callback(
        HttpContext context,
        string? code,
        string? state)
    {
        if (string.IsNullOrEmpty(code))
        {
            return Results.BadRequest(new ErrorResponse("No authorization code provided"));
        }

        var storedState = context.Session.GetString("oauth_state");
        context.Session.Remove("oauth_state");

        if (string.IsNullOrEmpty(state) || state != storedState)
        {
            logger.LogWarning("Invalid state parameter during OAuth callback.");
            return Results.BadRequest(new ErrorResponse("Invalid state parameter (CSRF protection)"));
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
        var redirectUri = reg.RedirectUri; // Should not be null if registered correctly

        try
        {
            var accessToken = await mastodonService.GetAccessTokenAsync(instance, clientId, clientSecret, code, redirectUri);
            var userId = await mastodonService.VerifyCredentialsAsync(instance, accessToken);

            // Create claims for the authenticated user
            var claims = ClaimsExtensions.CreateAuthenticationClaims(instance, accessToken?.Trim() ?? string.Empty, userId);
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            // Sign in the user with cookie authentication
            await context.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    AllowRefresh = true,
                });

            return Results.Redirect("/");
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "OAuth callback failed: {message}", ex.Message);
            return Results.BadRequest(new ErrorResponse($"OAuth failed: {ex.Message}"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "OAuth callback failed");
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Logs the user out by clearing authentication and session.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A redirect to the homepage.</returns>
    public async Task<IResult> Logout(HttpContext context)
    {
        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        context.Session.Clear();
        return Results.Redirect("/");
    }

    /// <summary>
    /// Registers the application with a Mastodon instance.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="request">The registration request.</param>
    /// <returns>The registration response.</returns>
    public async Task<IResult> Register(
        HttpContext context,
        RegisterRequest request)
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
            logger.LogWarning(ex, "App registration failed for {instance}: {message}", instance, ex.Message);
            return Results.BadRequest(new ErrorResponse($"Failed to register with instance: {ex.Message}"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "App registration failed for {instance}", instance);
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Returns the current authentication status.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>The status response.</returns>
    public IResult Status(HttpContext context)
    {
        var instance = context.User.GetInstance() ?? context.Session.GetString("instance");

        var registered = !string.IsNullOrEmpty(instance) && registrationStore.Has(instance);

        return Results.Ok(new StatusResponse(
            Authenticated: context.User.Identity?.IsAuthenticated ?? false,
            Instance: instance,
            Registered: registered));
    }
}
