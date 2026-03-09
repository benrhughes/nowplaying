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

public class MastodonAuthorizationHandlerTests
{
    private readonly MastodonAuthorizationHandler handler;
    private readonly Mock<ILogger<MastodonAuthorizationHandler>> loggerMock;
    private readonly MastodonRequirement requirement = new();

    public MastodonAuthorizationHandlerTests()
    {
        loggerMock = new Mock<ILogger<MastodonAuthorizationHandler>>();
        handler = new MastodonAuthorizationHandler(loggerMock.Object);
    }

    private AuthorizationHandlerContext CreateContext(ClaimsPrincipal principal, object? resource = null)
    {
        return new AuthorizationHandlerContext(new[] { requirement }, principal, resource);
    }

    [Fact]
    public async Task HandleRequirementAsync_NotAuthenticated_DoesNothing()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity()); // not authenticated
        var context = CreateContext(principal);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
        Assert.False(context.HasFailed);
    }

    [Theory]
    [InlineData(null, "token", "id")]
    [InlineData("inst", null, "id")]
    [InlineData("inst", "token", null)]
    public async Task HandleRequirementAsync_MissingClaim_Fails(string? instance, string? token, string? userId)
    {
        var claims = new List<Claim>();
        if (instance != null) {
            claims.Add(new Claim("instance", instance));
        }

        if (token != null) {
            claims.Add(new Claim("accessToken", token));
        }

        if (userId != null) {
            claims.Add(new Claim("userid", userId));
        }

        // authenticationType non-null means IsAuthenticated returns true
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        var context = CreateContext(principal);

        await handler.HandleAsync(context);

        Assert.True(context.HasFailed);
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleRequirementAsync_ValidClaimsWithoutHttpContext_Succeeds()
    {
        var claims = ClaimsExtensions.CreateAuthenticationClaims("inst", "token", "id");
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        var context = CreateContext(principal); // no resource

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirementAsync_ValidClaimsWithHttpContext_SucceedsAndSetsItem()
    {
        var claims = ClaimsExtensions.CreateAuthenticationClaims("inst", "token", "id");
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));

        var httpContext = new DefaultHttpContext();
        var context = CreateContext(principal, httpContext);

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
        Assert.Equal("id", httpContext.Items["MastodonUserId"]);
    }
}
