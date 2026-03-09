// Copyright (c) Ben Hughes. SPDX-License-Identifier: AGPL-3.0-or-later
using Microsoft.Extensions.Logging;
using Moq;
using NowPlaying.Services;
using Xunit;

namespace NowPlaying.Tests.Services;

/// <summary>
/// Unit tests for the <see cref="BandcampService"/> class.
/// </summary>
public class BandcampServiceTests
{
    /// <summary>
    /// Creates a new instance of the <see cref="BandcampService"/> with a mock handler.
    /// </summary>
    /// <param name="content">The HTML content to return.</param>
    /// <param name="statusCode">The HTTP status code to return.</param>
    /// <returns>A new <see cref="BandcampService"/> instance.</returns>
    private BandcampService CreateService(string? content, System.Net.HttpStatusCode statusCode = System.Net.HttpStatusCode.OK)
    {
        var httpClient = new HttpClient(new MockHttpMessageHandler(content, statusCode));
        var logger = new Mock<ILogger<BandcampService>>();
        return new BandcampService(httpClient, logger.Object);
    }

    /// <summary>
    /// Verifies that ScrapeAsync returns correctly parsed data for a valid Bandcamp URL.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
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
        var service = CreateService(htmlContent);

        // Act
        var result = await service.ScrapeAsync(url);

        // Assert
        Assert.Equal("Test Album – Test Artist", result.Title);
        Assert.Equal("Test Artist", result.Artist);
        Assert.Equal("Test Album", result.Album);
        Assert.Equal("https://example.com/image.jpg", result.Image);
        Assert.Equal("A test album description", result.Description);
        Assert.Equal(url, result.Url);
    }

    /// <summary>
    /// Verifies that artist and album are correctly parsed when using the "by" pattern.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
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
        var service = CreateService(htmlContent);

        // Act
        var result = await service.ScrapeAsync(url);

        // Assert
        Assert.Equal("Cool Artist", result.Artist);
        Assert.Equal("Awesome Album", result.Album);
    }

    /// <summary>
    /// Verifies that ScrapeAsync uses alternative tags when OG tags are missing.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
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
        var service = CreateService(htmlContent);

        // Act
        var result = await service.ScrapeAsync(url);

        // Assert
        Assert.Equal("Fallback Title", result.Title);
        Assert.Null(result.Image);
    }

    /// <summary>
    /// Verifies that an empty artist and album are returned when the title is not parseable.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
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
        var service = CreateService(htmlContent);

        // Act
        var result = await service.ScrapeAsync(url);

        // Assert
        Assert.Equal(string.Empty, result.Artist);
        Assert.Equal(string.Empty, result.Album);
    }

    /// <summary>
    /// Verifies that ScrapeAsync throws an exception on HTTP error.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task ScrapeAsync_WithHttpError_ThrowsException()
    {
        // Arrange
        var url = "https://example.bandcamp.com/album/test";
        var service = CreateService(null, System.Net.HttpStatusCode.NotFound);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => service.ScrapeAsync(url));
    }

    /// <summary>
    /// Verifies that trailing commas are trimmed from album names.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task ScrapeAsync_WithCommaInAlbumName_TrimsTrailingComma()
    {
        // Arrange
        var url = "https://example.bandcamp.com/album/test";
        var htmlContent = """
            <html>
            <head>
                <meta property="og:title" content="Test Album, by Test Artist" />
            </head>
            </html>
            """;
        var service = CreateService(htmlContent);

        // Act
        var result = await service.ScrapeAsync(url);

        // Assert
        Assert.Equal("Test Artist", result.Artist);
        Assert.Equal("Test Album", result.Album);
    }

    /// <summary>
    /// Mock HTTP message handler for testing.
    /// </summary>
    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly string? _content;
        private readonly System.Net.HttpStatusCode _statusCode;

        /// <summary>
        /// Initializes a new instance of the <see cref="MockHttpMessageHandler"/> class.
        /// </summary>
        /// <param name="content">The content to return.</param>
        /// <param name="statusCode">The status code to return.</param>
        public MockHttpMessageHandler(string? content, System.Net.HttpStatusCode statusCode = System.Net.HttpStatusCode.OK)
        {
            _content = content;
            _statusCode = statusCode;
        }

        /// <inheritdoc/>
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
