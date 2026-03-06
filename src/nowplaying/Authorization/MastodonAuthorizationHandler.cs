namespace NowPlaying.Authorization;

using Microsoft.AspNetCore.Authorization;
using NowPlaying.Extensions;

/// <summary>
/// Authorization handler that validates the presence of Mastodon claims.
/// </summary>
/// <param name="logger">The logger.</param>
#pragma warning disable SA1009 // StyleCop does not support primary constructors fully yet
public class MastodonAuthorizationHandler(ILogger<MastodonAuthorizationHandler> logger) : AuthorizationHandler<MastodonRequirement>
#pragma warning restore SA1009
{
    /// <summary>
    /// Handles the <see cref="MastodonRequirement"/> by verifying the Mastodon claims.
    /// </summary>
    /// <param name="context">The authorization context.</param>
    /// <param name="requirement">The requirement.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, MastodonRequirement requirement)
    {
        var user = context.User;
        if (user?.Identity == null || !user.Identity.IsAuthenticated)
        {
            // Not authenticated - let other handlers/pipeline respond
            return Task.CompletedTask;
        }

        var instance = user.GetInstance();
        var accessToken = user.GetAccessToken();
        var userId = user.GetUserId();

        if (string.IsNullOrEmpty(instance) || string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(userId))
        {
            logger.LogDebug("MastodonAuthorizationHandler: missing instance, access token, or user id in claims");
            context.Fail();
            return Task.CompletedTask;
        }

        // Store the Mastodon user id in HttpContext.Items for easy access downstream
        if (context.Resource is HttpContext httpContext)
        {
            httpContext.Items["MastodonUserId"] = userId;
        }

        context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
