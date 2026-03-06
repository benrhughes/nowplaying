namespace NowPlaying.Extensions;

using System.Security.Claims;

/// <summary>
/// Extensions for extracting custom claims from ClaimsPrincipal.
/// </summary>
public static class ClaimsExtensions
{
    private const string InstanceClaimType = "instance";
    private const string AccessTokenClaimType = "accessToken";

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
    /// Creates claims for instance and access token.
    /// </summary>
    /// <param name="instance">The Mastodon instance URL.</param>
    /// <param name="accessToken">The OAuth access token.</param>
    /// <returns>A list of claims.</returns>
    public static List<Claim> CreateAuthenticationClaims(string instance, string accessToken)
    {
        return new List<Claim>
        {
            new (InstanceClaimType, instance),
            new (AccessTokenClaimType, accessToken)
        };
    }
}
