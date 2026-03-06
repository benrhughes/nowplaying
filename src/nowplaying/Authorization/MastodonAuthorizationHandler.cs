namespace NowPlaying.Authorization;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using NowPlaying.Extensions;
using NowPlaying.Services;

/// <summary>
/// Authorization handler that verifies the Mastodon access token by calling the Mastodon API.
/// </summary>
/// <param name="mastodonService">The Mastodon service.</param>
/// <param name="logger">The logger.</param>
#pragma warning disable SA1009 // Closing parenthesis should not be followed by a space
public class MastodonAuthorizationHandler(IMastodonService mastodonService, ILogger<MastodonAuthorizationHandler> logger) : AuthorizationHandler<MastodonRequirement>
#pragma warning restore SA1009 // Closing parenthesis should not be followed by a space
{
    /// <summary>
    /// Handles the <see cref="MastodonRequirement"/> by verifying the Mastodon token.
    /// </summary>
    /// <param name="context">The authorization context.</param>
    /// <param name="requirement">The requirement.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, MastodonRequirement requirement)
    {
        var user = context.User;
        if (user?.Identity == null || !user.Identity.IsAuthenticated)
        {
            // Not authenticated - let other handlers/pipeline respond
            return;
        }

        var instance = user.GetInstance();
        var accessToken = user.GetAccessToken();

        if (string.IsNullOrEmpty(instance) || string.IsNullOrEmpty(accessToken))
        {
            logger.LogDebug("MastodonAuthorizationHandler: missing instance or access token in claims");
            return;
        }

        try
        {
            var userId = await mastodonService.VerifyCredentialsAsync(instance, accessToken);

            // Store the verified Mastodon user id in HttpContext.Items if available
            if (context.Resource is HttpContext httpContext)
            {
                httpContext.Items["MastodonUserId"] = userId;
            }

            context.Succeed(requirement);
        }
        catch (HttpRequestException ex)
        {
            // If the token is invalid/expired, explicitly fail the requirement and
            // convert a 401 response into an UnauthorizedAccessException so the
            // global exception handler can translate that into an HTTP 401.
            logger.LogWarning(ex, "Mastodon token verification failed: {Message}", ex.Message);

            if (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                // attempt to sign the user out to clear stale cookies/session
                if (context.Resource is HttpContext httpContext)
                {
                    try
                    {
                        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    }
                    catch (Exception signOutEx)
                    {
                        logger.LogDebug(signOutEx, "Sign-out during auth failure failed");
                    }
                }

                // fail the requirement and surface an UnauthorizedAccessException
                context.Fail();
                throw new UnauthorizedAccessException("Mastodon access token is invalid or revoked.");
            }

            // For other HTTP errors, fail the requirement but do not throw so that
            // the request can be handled as an authorization failure (403) or by
            // other middleware as appropriate.
            context.Fail();
        }
        catch (Exception ex)
        {
            // Unexpected errors should be logged and the requirement failed so
            // the pipeline can decide how to respond without causing an
            // unhandled exception.
            logger.LogError(ex, "Unexpected error while verifying Mastodon token");
            context.Fail();
        }
    }
}
