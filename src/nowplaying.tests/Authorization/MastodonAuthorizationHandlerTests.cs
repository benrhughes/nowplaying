// Copyright (c) Ben Hughes. SPDX-License-Identifier: AGPL-3.0-or-later
namespace NowPlaying.Tests.Authorization;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using NowPlaying.Authorization;
using NowPlaying.Extensions;
using Xunit;

/// <summary>
/// Unit tests for the <see cref="MastodonAuthorizationHandler"/> class.
/// </summary>
public class MastodonAuthorizationHandlerTests
{
    private readonly MastodonAuthorizationHandler _handler;
    private readonly Mock<ILogger<MastodonAuthorizationHandler>> _loggerMock;
    private readonly MastodonRequirement _requirement = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="MastodonAuthorizationHandlerTests"/> class.
    /// </summary>
    public MastodonAuthorizationHandlerTests()
    {
        _loggerMock = new Mock<ILogger<MastodonAuthorizationHandler>>();
        _handler = new MastodonAuthorizationHandler(_loggerMock.Object);
    }

    /// <summary>
    /// Creates an authorization handler context for testing.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    /// <param name="resource">The resource being authorized.</param>
    /// <returns>A new <see cref="AuthorizationHandlerContext"/>.</returns>
    private AuthorizationHandlerContext CreateContext(ClaimsPrincipal principal, object? resource = null)
    {
        return new AuthorizationHandlerContext(new[] { _requirement }, principal, resource);
    }

    /// <summary>
    /// Verifies that the requirement does not succeed if the user is not authenticated.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task HandleRequirementAsync_NotAuthenticated_DoesNothing()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity()); // not authenticated
        var context = CreateContext(principal);

        await _handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
        Assert.False(context.HasFailed);
    }

    /// <summary>
    /// Verifies that the requirement fails if any required claim is missing.
    /// </summary>
    /// <param name="instance">The instance claim value.</param>
    /// <param name="token">The token claim value.</param>
    /// <param name="userId">The user ID claim value.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Theory]
    [InlineData(null, "token", "id")]
    [InlineData("inst", null, "id")]
    [InlineData("inst", "token", null)]
    public async Task HandleRequirementAsync_MissingClaim_Fails(string? instance, string? token, string? userId)
    {
        var claims = new List<Claim>();
        if (instance != null)
        {
            claims.Add(new Claim("instance", instance));
        }

        if (token != null)
        {
            claims.Add(new Claim("accessToken", token));
        }

        if (userId != null)
        {
            claims.Add(new Claim("userid", userId));
        }

        // authenticationType non-null means IsAuthenticated returns true
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        var context = CreateContext(principal);

        await _handler.HandleAsync(context);

        Assert.True(context.HasFailed);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Verifies that the requirement succeeds when all valid claims are present, even without HttpContext.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task HandleRequirementAsync_ValidClaimsWithoutHttpContext_Succeeds()
    {
        var claims = ClaimsExtensions.CreateAuthenticationClaims("inst", "token", "id");
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        var context = CreateContext(principal); // no resource

        await _handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    /// <summary>
    /// Verifies that the requirement succeeds and sets the user ID in HttpContext items when valid claims are present.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task HandleRequirementAsync_ValidClaimsWithHttpContext_SucceedsAndSetsItem()
    {
        var claims = ClaimsExtensions.CreateAuthenticationClaims("inst", "token", "id");
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));

        var httpContext = new DefaultHttpContext();
        var context = CreateContext(principal, httpContext);

        await _handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
        Assert.Equal("id", httpContext.Items["MastodonUserId"]);
    }
}
