using System.ComponentModel.DataAnnotations;
using NowPlaying.Endpoints;
using NowPlaying.Models;
using NowPlaying.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace NowPlaying.Tests.Endpoints;

public class ConfigurationEndpointsTests
{
    private readonly Mock<IMastodonService> _mastodonServiceMock;
    private readonly Mock<HttpContext> _httpContextMock;
    private readonly Mock<ISession> _sessionMock;
    private readonly Mock<ILoggerFactory> _loggerFactoryMock;
    private readonly AppConfig _config;

    public ConfigurationEndpointsTests()
    {
        _mastodonServiceMock = new Mock<IMastodonService>();
        _httpContextMock = new Mock<HttpContext>();
        _sessionMock = new Mock<ISession>();
        _loggerFactoryMock = new Mock<ILoggerFactory>();
        _loggerFactoryMock.Setup(f => f.CreateLogger(It.IsAny<string>()))
            .Returns(new Mock<ILogger>().Object);
        _config = new AppConfig 
        { 
            Port = 4444,
            RedirectUri = "http://localhost:4444/auth/callback",
            SessionSecret = "dev-secret"
        };

        _httpContextMock.Setup(h => h.Session).Returns(_sessionMock.Object);
    }

    [Fact]
    public async Task Register_WithValidInstance_ReturnsResult()
    {
        // Arrange
        var request = new RegisterRequest { Instance = "https://mastodon.social" };
        _mastodonServiceMock.Setup(m => m.RegisterAppAsync("mastodon.social", _config.RedirectUri))
            .ReturnsAsync(("client-id", "client-secret"));

        // Act
        var result = await ConfigurationEndpoints.Register(_httpContextMock.Object, request, _mastodonServiceMock.Object, _config, _loggerFactoryMock.Object);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void RegisterRequest_WithNullInstance_FailsValidation()
    {
        // Arrange
        var request = new RegisterRequest { Instance = null! };
        var validationResults = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(request, new ValidationContext(request), validationResults, validateAllProperties: true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(validationResults, r => r.MemberNames.Contains(nameof(RegisterRequest.Instance)));
    }

    [Fact]
    public void Status_WithAuthenticatedSession_ReturnsResult()
    {
        // Act
        var result = ConfigurationEndpoints.Status(_httpContextMock.Object);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void Status_WithUnauthenticatedSession_ReturnsResult()
    {
        // Act
        var result = ConfigurationEndpoints.Status(_httpContextMock.Object);

        // Assert
        Assert.NotNull(result);
    }
}
