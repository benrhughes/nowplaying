// Copyright (c) Ben Hughes. SPDX-License-Identifier: AGPL-3.0-or-later
namespace NowPlaying.Extensions;

using System.Security.Claims;

/// <summary>
/// Extensions for extracting custom claims from ClaimsPrincipal.
/// </summary>
public static class ClaimsExtensions
{
    private const string InstanceClaimType = "instance";
    private const string AccessTokenClaimType = "accessToken";
    private const string UserIdClaimType = "userid";

    /// <summary>
    /// Extracts the Mastodon instance URL from claims.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    /// <returns>The instance URL or null if not found.</returns>
    public static string? GetInstance(this ClaimsPrincipal principal)
    {
        if (principal == null)
        {
            return null;
        }

        return principal.FindFirst(InstanceClaimType)?.Value;
    }

    /// <summary>
    /// Extracts the Mastodon access token from claims.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    /// <returns>The access token or null if not found.</returns>
    public static string? GetAccessToken(this ClaimsPrincipal principal)
    {
        if (principal == null)
        {
            return null;
        }

        return principal.FindFirst(AccessTokenClaimType)?.Value;
    }

    /// <summary>
    /// Extracts the Mastodon user ID from claims.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    /// <returns>The user ID or null if not found.</returns>
    public static string? GetUserId(this ClaimsPrincipal principal)
    {
        if (principal == null)
        {
            return null;
        }

        return principal.FindFirst(UserIdClaimType)?.Value;
    }

    /// <summary>
    /// Creates claims for instance, access token, and user ID.
    /// </summary>
    /// <param name="instance">The Mastodon instance URL.</param>
    /// <param name="accessToken">The OAuth access token.</param>
    /// <param name="userId">The Mastodon user ID.</param>
    /// <returns>A list of claims.</returns>
    public static List<Claim> CreateAuthenticationClaims(string instance, string accessToken, string userId)
    {
        return
        [
            new (InstanceClaimType, instance),
            new (AccessTokenClaimType, accessToken),
            new (UserIdClaimType, userId)
        ];
    }
}
