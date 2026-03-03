using BcMasto.Endpoints;
using BcMasto.Models;
using BcMasto.Services;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace BcMasto.Tests.Endpoints;

public class AuthEndpointsTests
{
    private readonly Mock<IMastodonService> _mastodonServiceMock;
    private readonly Mock<HttpContext> _httpContextMock;
    private readonly Mock<ISession> _sessionMock;

    public AuthEndpointsTests()
    {
        _mastodonServiceMock = new Mock<IMastodonService>();
        _httpContextMock = new Mock<HttpContext>();
        _sessionMock = new Mock<ISession>();

        _httpContextMock.Setup(h => h.Session).Returns(_sessionMock.Object);
    }

    [Fact]
    public void Login_WithSessionInstance_ReturnsResult()
    {
        // Act
        var result = AuthEndpoints.Login(_httpContextMock.Object, "mastodon.social", "client-id");

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void Login_WithParameterInstance_ReturnsResult()
    {
        // Act
        var result = AuthEndpoints.Login(_httpContextMock.Object, "mastodon.social", "client-id");

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void Login_WithMissingInstance_ReturnsResult()
    {
        // Act
        var result = AuthEndpoints.Login(_httpContextMock.Object, null, null);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void Login_WithMissingClientId_ReturnsResult()
    {
        // Act
        var result = AuthEndpoints.Login(_httpContextMock.Object, "mastodon.social", null);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Callback_WithValidCode_ReturnsResult()
    {
        // Arrange
        _mastodonServiceMock.Setup(m => m.GetAccessTokenAsync("mastodon.social", "client-id", "client-secret", "auth-code"))
            .ReturnsAsync("access-token");

        // Act
        var result = await AuthEndpoints.Callback(_httpContextMock.Object, "auth-code", _mastodonServiceMock.Object);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Callback_WithNullCode_ReturnsResult()
    {
        // Arrange & Act
        var result = await AuthEndpoints.Callback(_httpContextMock.Object, null, _mastodonServiceMock.Object);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Callback_WithMissingInstance_ReturnsResult()
    {
        // Arrange & Act
        var result = await AuthEndpoints.Callback(_httpContextMock.Object, "auth-code", _mastodonServiceMock.Object);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Callback_WithMissingClientId_ReturnsResult()
    {
        // Arrange & Act
        var result = await AuthEndpoints.Callback(_httpContextMock.Object, "auth-code", _mastodonServiceMock.Object);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Callback_WithMissingClientSecret_ReturnsResult()
    {
        // Arrange & Act
        var result = await AuthEndpoints.Callback(_httpContextMock.Object, "auth-code", _mastodonServiceMock.Object);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Callback_WithServiceError_ReturnsResult()
    {
        // Arrange
        _mastodonServiceMock.Setup(m => m.GetAccessTokenAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new HttpRequestException("Service error"));

        // Act
        var result = await AuthEndpoints.Callback(_httpContextMock.Object, "auth-code", _mastodonServiceMock.Object);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void Logout_ClearsSessionAndReturnsResult()
    {
        // Arrange & Act
        var result = AuthEndpoints.Logout(_httpContextMock.Object);

        // Assert
        Assert.NotNull(result);
        _sessionMock.Verify(s => s.Clear(), Times.Once);
    }
}
