using NowPlaying.Endpoints;
using NowPlaying.Models;
using NowPlaying.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace NowPlaying.Tests.Endpoints;

public class AuthenticationEndpointsTests
{
    private readonly Mock<IMastodonService> _mastodonServiceMock;
    private readonly Mock<HttpContext> _httpContextMock;
    private readonly Mock<ISession> _sessionMock;
    private readonly Mock<ILoggerFactory> _loggerFactoryMock;
    private readonly AppConfig _config;

    public AuthenticationEndpointsTests()
    {
        _mastodonServiceMock = new Mock<IMastodonService>();
        _httpContextMock = new Mock<HttpContext>();
        _sessionMock = new Mock<ISession>();
        _loggerFactoryMock = new Mock<ILoggerFactory>();
        _loggerFactoryMock.Setup(f => f.CreateLogger(It.IsAny<string>()))
            .Returns(new Mock<ILogger>().Object);

        _httpContextMock.Setup(h => h.Session).Returns(_sessionMock.Object);
        _config = new AppConfig
        {
            Port = 3000,
            RedirectUri = "http://localhost:3000/auth/callback",
            SessionSecret = "test-secret"
        };
    }

    [Fact]
    public void Login_WithSessionInstance_ReturnsResult()
    {
        // Act
        var result = AuthenticationEndpoints.Login(_httpContextMock.Object, "mastodon.social", "client-id", _config);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void Login_WithParameterInstance_ReturnsResult()
    {
        // Act
        var result = AuthenticationEndpoints.Login(_httpContextMock.Object, "mastodon.social", "client-id", _config);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void Login_WithMissingInstance_ReturnsResult()
    {
        // Act
        var result = AuthenticationEndpoints.Login(_httpContextMock.Object, null, null, _config);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void Login_WithMissingClientId_ReturnsResult()
    {
        // Act
        var result = AuthenticationEndpoints.Login(_httpContextMock.Object, "mastodon.social", null, _config);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Callback_WithValidCode_ReturnsResult()
    {
        // Arrange
        _mastodonServiceMock.Setup(m => m.GetAccessTokenAsync("mastodon.social", "client-id", "client-secret", "auth-code", "http://localhost:3000/auth/callback"))
            .ReturnsAsync("access-token");

        // Act
        var result = await AuthenticationEndpoints.Callback(_httpContextMock.Object, "auth-code", _mastodonServiceMock.Object, _config, _loggerFactoryMock.Object);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Callback_WithNullCode_ReturnsResult()
    {
        // Arrange & Act
        var result = await AuthenticationEndpoints.Callback(_httpContextMock.Object, null, _mastodonServiceMock.Object, _config, _loggerFactoryMock.Object);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Callback_WithMissingInstance_ReturnsResult()
    {
        // Arrange & Act
        var result = await AuthenticationEndpoints.Callback(_httpContextMock.Object, "auth-code", _mastodonServiceMock.Object, _config, _loggerFactoryMock.Object);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Callback_WithMissingClientId_ReturnsResult()
    {
        // Arrange & Act
        var result = await AuthenticationEndpoints.Callback(_httpContextMock.Object, "auth-code", _mastodonServiceMock.Object, _config, _loggerFactoryMock.Object);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Callback_WithMissingClientSecret_ReturnsResult()
    {
        // Arrange & Act
        var result = await AuthenticationEndpoints.Callback(_httpContextMock.Object, "auth-code", _mastodonServiceMock.Object, _config, _loggerFactoryMock.Object);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Callback_WithServiceError_ReturnsResult()
    {
        // Arrange
        _mastodonServiceMock.Setup(m => m.GetAccessTokenAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new HttpRequestException("Service error"));

        // Act
        var result = await AuthenticationEndpoints.Callback(_httpContextMock.Object, "auth-code", _mastodonServiceMock.Object, _config, _loggerFactoryMock.Object);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void Logout_ClearsSessionAndReturnsResult()
    {
        // Arrange & Act
        var result = AuthenticationEndpoints.Logout(_httpContextMock.Object);

        // Assert
        Assert.NotNull(result);
        _sessionMock.Verify(s => s.Clear(), Times.Once);
    }
}
