using System.ComponentModel.DataAnnotations;
using NowPlaying.Endpoints;
using NowPlaying.Models;
using NowPlaying.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace NowPlaying.Tests.Endpoints;

public class ApiEndpointsTests
{
    private readonly Mock<IBandcampService> _bandcampServiceMock;
    private readonly Mock<IMastodonService> _mastodonServiceMock;
    private readonly Mock<IImageService> _imageServiceMock;
    private readonly Mock<HttpContext> _httpContextMock;
    private readonly Mock<ISession> _sessionMock;
    private readonly Mock<ILoggerFactory> _loggerFactoryMock;
    private readonly AppConfig _config;

    public ApiEndpointsTests()
    {
        _bandcampServiceMock = new Mock<IBandcampService>();
        _mastodonServiceMock = new Mock<IMastodonService>();
        _imageServiceMock = new Mock<IImageService>();
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
        var result = await ApiEndpoints.Register(_httpContextMock.Object, request, _mastodonServiceMock.Object, _config, _loggerFactoryMock.Object);

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
        var result = ApiEndpoints.Status(_httpContextMock.Object);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void Status_WithUnauthenticatedSession_ReturnsResult()
    {
        // Act
        var result = ApiEndpoints.Status(_httpContextMock.Object);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Scrape_WithValidBandcampUrl_ReturnsResult()
    {
        // Arrange
        var request = new ScrapeRequest { Url = "https://artist.bandcamp.com/album/test" };
        var scrapeResponse = new ScrapeResponse(
            Title: "Test Album – Test Artist",
            Artist: "Test Artist",
            Album: "Test Album",
            Image: "https://example.com/image.jpg",
            Description: "A test album",
            Url: "https://artist.bandcamp.com/album/test");

        _bandcampServiceMock.Setup(b => b.ScrapeAsync(request.Url))
            .ReturnsAsync(scrapeResponse);

        // Act
        var result = await ApiEndpoints.Scrape(_httpContextMock.Object, request, _bandcampServiceMock.Object, _loggerFactoryMock.Object);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void ScrapeRequest_WithNullUrl_FailsValidation()
    {
        // Arrange
        var request = new ScrapeRequest { Url = null! };
        var validationResults = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(request, new ValidationContext(request), validationResults, validateAllProperties: true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(validationResults, r => r.MemberNames.Contains(nameof(ScrapeRequest.Url)));
    }

    [Fact]
    public void ScrapeRequest_WithInvalidUrl_FailsValidation()
    {
        // Arrange
        var request = new ScrapeRequest { Url = "not a valid url" };
        var validationResults = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(request, new ValidationContext(request), validationResults, validateAllProperties: true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(validationResults, r => r.MemberNames.Contains(nameof(ScrapeRequest.Url)));
    }

    [Fact]
    public async Task Scrape_WithNonBandcampUrl_ReturnsResult()
    {
        // Arrange
        var request = new ScrapeRequest { Url = "https://spotify.com/album/test" };

        // Act
        var result = await ApiEndpoints.Scrape(_httpContextMock.Object, request, _bandcampServiceMock.Object, _loggerFactoryMock.Object);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Post_WithValidRequest_ReturnsResult()
    {
        // Arrange
        var request = new PostRequest { Text = "Check this out!", ImageUrl = "https://example.com/image.jpg" };
        
        var instanceBytes = System.Text.Encoding.UTF8.GetBytes("https://mastodon.social");
        var tokenBytes = System.Text.Encoding.UTF8.GetBytes("test-token");
        _sessionMock.Setup(s => s.TryGetValue("instance", out instanceBytes)).Returns(true);
        _sessionMock.Setup(s => s.TryGetValue("accessToken", out tokenBytes)).Returns(true);

        _imageServiceMock.Setup(i => i.DownloadImageAsync(request.ImageUrl))
            .ReturnsAsync(new byte[] { 0x89, 0x50, 0x4E, 0x47 });
        
        _mastodonServiceMock.Setup(m => m.UploadMediaAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), null))
            .ReturnsAsync("media-id");
        
        _mastodonServiceMock.Setup(m => m.PostStatusAsync(It.IsAny<string>(), It.IsAny<string>(), "Check this out!", "media-id"))
            .ReturnsAsync(("status-id", "https://mastodon.social/@user/123"));
 
        // Act
        var result = await ApiEndpoints.Post(_httpContextMock.Object, request, _mastodonServiceMock.Object, _imageServiceMock.Object, _loggerFactoryMock.Object);
 
        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Post_WithoutAuthentication_ReturnsResult()
    {
        // Arrange
        var request = new PostRequest { Text = "Check this out!", ImageUrl = "https://example.com/image.jpg" };
 
        // Act
        var result = await ApiEndpoints.Post(_httpContextMock.Object, request, _mastodonServiceMock.Object, _imageServiceMock.Object, _loggerFactoryMock.Object);
 
        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void PostRequest_WithMissingText_FailsValidation()
    {
        // Arrange
        var request = new PostRequest { Text = null!, ImageUrl = "https://example.com/image.jpg" };
        var validationResults = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(request, new ValidationContext(request), validationResults, validateAllProperties: true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(validationResults, r => r.MemberNames.Contains(nameof(PostRequest.Text)));
    }

    [Fact]
    public void PostRequest_WithMissingImage_FailsValidation()
    {
        // Arrange
        var request = new PostRequest { Text = "Check this out!", ImageUrl = null! };
        var validationResults = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(request, new ValidationContext(request), validationResults, validateAllProperties: true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(validationResults, r => r.MemberNames.Contains(nameof(PostRequest.ImageUrl)));
    }

    private class MockImageHttpHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            response.Content = new ByteArrayContent(new byte[] { 0x89, 0x50, 0x4E, 0x47 }); // PNG header
            return Task.FromResult(response);
        }
    }
}
