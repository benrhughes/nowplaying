using BcMasto.Models;
using BcMasto.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BcMasto.Tests.Services;

public class BandcampServiceTests
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<ILogger<BandcampService>> _loggerMock;
    private readonly BandcampService _service;

    public BandcampServiceTests()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _loggerMock = new Mock<ILogger<BandcampService>>();
        _service = new BandcampService(_httpClientFactoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task ScrapeAsync_WithValidUrl_ReturnsScrapedData()
    {
        // Arrange
        var url = "https://example.bandcamp.com/album/test";
        var htmlContent = """
            <html>
            <head>
                <meta property="og:title" content="Test Album – Test Artist" />
                <meta property="og:image" content="https://example.com/image.jpg" />
                <meta property="og:description" content="A test album description" />
            </head>
            </html>
            """;

        var httpClient = new HttpClient(new MockHttpMessageHandler(htmlContent));
        _httpClientFactoryMock.Setup(f => f.CreateClient("Default")).Returns(httpClient);

        // Act
        var result = await _service.ScrapeAsync(url);

        // Assert
        Assert.Equal("Test Album – Test Artist", result.Title);
        Assert.Equal("Test Artist", result.Artist);
        Assert.Equal("Test Album", result.Album);
        Assert.Equal("https://example.com/image.jpg", result.Image);
        Assert.Equal("A test album description", result.Description);
        Assert.Equal(url, result.Url);
    }

    [Fact]
    public async Task ScrapeAsync_WithByPattern_ParsesArtistAndAlbum()
    {
        // Arrange
        var url = "https://example.bandcamp.com/album/test";
        var htmlContent = """
            <html>
            <head>
                <meta property="og:title" content="Awesome Album by Cool Artist" />
                <meta property="og:image" content="https://example.com/image.jpg" />
                <meta property="og:description" content="Description" />
            </head>
            </html>
            """;

        var httpClient = new HttpClient(new MockHttpMessageHandler(htmlContent));
        _httpClientFactoryMock.Setup(f => f.CreateClient("Default")).Returns(httpClient);

        // Act
        var result = await _service.ScrapeAsync(url);

        // Assert
        Assert.Equal("Cool Artist", result.Artist);
        Assert.Equal("Awesome Album", result.Album);
    }

    [Fact]
    public async Task ScrapeAsync_WithMissingOgTags_UsesAlternatives()
    {
        // Arrange
        var url = "https://example.bandcamp.com/album/test";
        var htmlContent = """
            <html>
            <head>
                <title>Fallback Title</title>
                <meta property="og:description" content="Description" />
            </head>
            </html>
            """;

        var httpClient = new HttpClient(new MockHttpMessageHandler(htmlContent));
        _httpClientFactoryMock.Setup(f => f.CreateClient("Default")).Returns(httpClient);

        // Act
        var result = await _service.ScrapeAsync(url);

        // Assert
        Assert.Equal("Fallback Title", result.Title);
        Assert.Null(result.Image);
    }

    [Fact]
    public async Task ScrapeAsync_WithoutParseableTitle_ReturnsEmptyArtistAndAlbum()
    {
        // Arrange
        var url = "https://example.bandcamp.com/album/test";
        var htmlContent = """
            <html>
            <head>
                <meta property="og:title" content="Just A Title" />
                <meta property="og:image" content="https://example.com/image.jpg" />
                <meta property="og:description" content="Description" />
            </head>
            </html>
            """;

        var httpClient = new HttpClient(new MockHttpMessageHandler(htmlContent));
        _httpClientFactoryMock.Setup(f => f.CreateClient("Default")).Returns(httpClient);

        // Act
        var result = await _service.ScrapeAsync(url);

        // Assert
        Assert.Equal("", result.Artist);
        Assert.Equal("", result.Album);
    }

    [Fact]
    public async Task ScrapeAsync_WithHttpError_ThrowsException()
    {
        // Arrange
        var url = "https://example.bandcamp.com/album/test";
        var httpClient = new HttpClient(new MockHttpMessageHandler(null, System.Net.HttpStatusCode.NotFound));
        _httpClientFactoryMock.Setup(f => f.CreateClient("Default")).Returns(httpClient);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => _service.ScrapeAsync(url));
    }

    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly string? _content;
        private readonly System.Net.HttpStatusCode _statusCode;

        public MockHttpMessageHandler(string? content, System.Net.HttpStatusCode statusCode = System.Net.HttpStatusCode.OK)
        {
            _content = content;
            _statusCode = statusCode;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(_statusCode);
            if (_content != null)
            {
                response.Content = new StringContent(_content);
            }
            return Task.FromResult(response);
        }
    }
}
