// Copyright (c) Ben Hughes. SPDX-License-Identifier: AGPL-3.0-or-later
namespace NowPlaying.Tests.Extensions;

using System.Security.Claims;
using NowPlaying.Extensions;
using Xunit;

/// <summary>
/// Verifies the helpers that read and create Mastodon-specific claims.
/// </summary>
public class ClaimsExtensionsTests
{
    /// <summary>
    /// Verifies that instance, access token, and user ID claims are correctly retrieved.
    /// </summary>
    [Fact]
    public void GetInstance_AccessToken_UserId_ReturnValues()
    {
        var claims = ClaimsExtensions.CreateAuthenticationClaims("https://mastodon.social", "token", "userid123");
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));

        Assert.Equal("https://mastodon.social", principal.GetInstance());
        Assert.Equal("token", principal.GetAccessToken());
        Assert.Equal("userid123", principal.GetUserId());
    }

    /// <summary>
    /// Verifies that retrieval methods return null when the principal is null or claims are missing.
    /// </summary>
    [Fact]
    public void GetMethods_ReturnNull_WhenPrincipalIsNullOrClaimsMissing()
    {
        // exercise null principal via static method to avoid nullable warnings
        Assert.Null(ClaimsExtensions.GetInstance(null!));
        Assert.Null(ClaimsExtensions.GetAccessToken(null!));
        Assert.Null(ClaimsExtensions.GetUserId(null!));

        var empty = new ClaimsPrincipal();
        Assert.Null(empty.GetInstance());
        Assert.Null(empty.GetAccessToken());
        Assert.Null(empty.GetUserId());
    }

    /// <summary>
    /// Verifies that CreateAuthenticationClaims produces the correct claim types and values.
    /// </summary>
    [Fact]
    public void CreateAuthenticationClaims_ProducesCorrectClaimTypes()
    {
        var list = ClaimsExtensions.CreateAuthenticationClaims("i", "a", "u");
        Assert.Contains(list, c => c.Type == "instance" && c.Value == "i");
        Assert.Contains(list, c => c.Type == "accessToken" && c.Value == "a");
        Assert.Contains(list, c => c.Type == "userid" && c.Value == "u");
    }
}
