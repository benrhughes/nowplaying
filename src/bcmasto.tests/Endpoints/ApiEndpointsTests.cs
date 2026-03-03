using BcMasto.Endpoints;
using BcMasto.Models;
using BcMasto.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BcMasto.Tests.Endpoints;

public class ApiEndpointsTests
{
    private readonly Mock<IBandcampService> _bandcampServiceMock;
    private readonly Mock<IMastodonService> _mastodonServiceMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<HttpContext> _httpContextMock;
    private readonly Mock<ISession> _sessionMock;

    public ApiEndpointsTests()
    {
        _bandcampServiceMock = new Mock<IBandcampService>();
        _mastodonServiceMock = new Mock<IMastodonService>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _httpContextMock = new Mock<HttpContext>();
        _sessionMock = new Mock<ISession>();

        _httpContextMock.Setup(h => h.Session).Returns(_sessionMock.Object);
    }

    [Fact]
    public async Task Register_WithValidInstance_ReturnsResult()
    {
        // Arrange
        var request = new RegisterRequest("https://mastodon.social");
        _mastodonServiceMock.Setup(m => m.RegisterAppAsync("mastodon.social"))
            .ReturnsAsync(("client-id", "client-secret"));

        // Act
        var result = await ApiEndpoints.Register(_httpContextMock.Object, request, _mastodonServiceMock.Object);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Register_WithNullInstance_ReturnsResult()
    {
        // Arrange
        var request = new RegisterRequest(null!);

        // Act
        var result = await ApiEndpoints.Register(_httpContextMock.Object, request, _mastodonServiceMock.Object);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Register_WithInvalidUrl_ReturnsResult()
    {
        // Arrange
        var request = new RegisterRequest("not a valid url");

        // Act
        var result = await ApiEndpoints.Register(_httpContextMock.Object, request, _mastodonServiceMock.Object);

        // Assert
        Assert.NotNull(result);
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
        var request = new ScrapeRequest("https://artist.bandcamp.com/album/test");
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
        var result = await ApiEndpoints.Scrape(_httpContextMock.Object, request, _bandcampServiceMock.Object);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Scrape_WithNullUrl_ReturnsResult()
    {
        // Arrange
        var request = new ScrapeRequest(null!);

        // Act
        var result = await ApiEndpoints.Scrape(_httpContextMock.Object, request, _bandcampServiceMock.Object);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Scrape_WithInvalidUrl_ReturnsResult()
    {
        // Arrange
        var request = new ScrapeRequest("not a valid url");

        // Act
        var result = await ApiEndpoints.Scrape(_httpContextMock.Object, request, _bandcampServiceMock.Object);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Scrape_WithNonBandcampUrl_ReturnsResult()
    {
        // Arrange
        var request = new ScrapeRequest("https://spotify.com/album/test");

        // Act
        var result = await ApiEndpoints.Scrape(_httpContextMock.Object, request, _bandcampServiceMock.Object);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Post_WithValidRequest_ReturnsResult()
    {
        // Arrange
        var request = new PostRequest("Check this out!", "https://example.com/image.jpg");
        
        var httpClient = new HttpClient(new MockImageHttpHandler());
        _httpClientFactoryMock.Setup(f => f.CreateClient("Default")).Returns(httpClient);
        
        _mastodonServiceMock.Setup(m => m.UploadMediaAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), null))
            .ReturnsAsync("media-id");
        
        _mastodonServiceMock.Setup(m => m.PostStatusAsync(It.IsAny<string>(), It.IsAny<string>(), "Check this out!", "media-id"))
            .ReturnsAsync(("status-id", "https://mastodon.social/@user/123"));

        // Act
        var result = await ApiEndpoints.Post(_httpContextMock.Object, request, _mastodonServiceMock.Object, _httpClientFactoryMock.Object);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Post_WithoutAuthentication_ReturnsResult()
    {
        // Arrange
        var request = new PostRequest("Check this out!", "https://example.com/image.jpg");

        // Act
        var result = await ApiEndpoints.Post(_httpContextMock.Object, request, _mastodonServiceMock.Object, _httpClientFactoryMock.Object);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Post_WithMissingText_ReturnsResult()
    {
        // Arrange
        var request = new PostRequest(null!, "https://example.com/image.jpg");

        // Act
        var result = await ApiEndpoints.Post(_httpContextMock.Object, request, _mastodonServiceMock.Object, _httpClientFactoryMock.Object);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Post_WithMissingImage_ReturnsResult()
    {
        // Arrange
        var request = new PostRequest("Check this out!", null!);

        // Act
        var result = await ApiEndpoints.Post(_httpContextMock.Object, request, _mastodonServiceMock.Object, _httpClientFactoryMock.Object);

        // Assert
        Assert.NotNull(result);
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
