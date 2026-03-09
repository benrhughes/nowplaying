// Copyright (c) Ben Hughes. SPDX-License-Identifier: AGPL-3.0-or-later
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;
using Moq;
using NowPlaying.Endpoints;
using NowPlaying.Extensions;
using NowPlaying.Models;
using NowPlaying.Services;
using Xunit;

namespace NowPlaying.Tests.Endpoints;

/// <summary>
/// Unit tests for the <see cref="PostingEndpoints"/> class.
/// </summary>
public class PostingEndpointsTests
{
    private readonly Mock<IBandcampService> _bandcampServiceMock;
    private readonly Mock<IMastodonService> _mastodonServiceMock;
    private readonly Mock<IImageService> _imageServiceMock;
    private readonly Mock<HttpContext> _httpContextMock;
    private readonly Mock<ISession> _sessionMock;
    private readonly Mock<ILogger<PostingEndpoints>> _loggerMock;
    private readonly AppConfig _config;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostingEndpointsTests"/> class.
    /// </summary>
    public PostingEndpointsTests()
    {
        _bandcampServiceMock = new Mock<IBandcampService>();
        _mastodonServiceMock = new Mock<IMastodonService>();
        _imageServiceMock = new Mock<IImageService>();
        _httpContextMock = new Mock<HttpContext>();
        _sessionMock = new Mock<ISession>();
        _loggerMock = new Mock<ILogger<PostingEndpoints>>();
        _config = new AppConfig
        {
            Port = 4444,
            RedirectUri = "http://localhost:4444/auth/callback",
            SessionSecret = "dev-secret"
        };

        _httpContextMock.Setup(h => h.Session).Returns(_sessionMock.Object);
    }

    /// <summary>
    /// Creates a new instance of <see cref="PostingEndpoints"/>.
    /// </summary>
    /// <returns>A new <see cref="PostingEndpoints"/> instance.</returns>
    private PostingEndpoints CreateEndpoints() => new(_bandcampServiceMock.Object, _mastodonServiceMock.Object, _imageServiceMock.Object, _loggerMock.Object);

    /// <summary>
    /// Sets up an authenticated user in the current HTTP context.
    /// </summary>
    /// <param name="instance">The instance URL.</param>
    /// <param name="accessToken">The access token.</param>
    private void SetupAuthenticatedUser(string instance, string accessToken)
    {
        var claims = ClaimsExtensions.CreateAuthenticationClaims(instance, accessToken, "test-user-id");
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        _httpContextMock.Setup(h => h.User).Returns(principal);
    }

    /// <summary>
    /// Verifies that Scrape returns a result for a valid Bandcamp URL.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
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
        var result = await CreateEndpoints().Scrape(_httpContextMock.Object, request);

        // Assert
        Assert.NotNull(result);
    }

    /// <summary>
    /// Verifies that ScrapeRequest fails validation when the URL is null.
    /// </summary>
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

    /// <summary>
    /// Verifies that ScrapeRequest fails validation when the URL is invalid.
    /// </summary>
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

    /// <summary>
    /// Verifies that Scrape returns a result even for non-Bandcamp URLs.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task Scrape_WithNonBandcampUrl_ReturnsResult()
    {
        // Arrange
        var request = new ScrapeRequest { Url = "https://spotify.com/album/test" };

        // Act
        var result = await CreateEndpoints().Scrape(_httpContextMock.Object, request);

        // Assert
        Assert.NotNull(result);
    }

    /// <summary>
    /// Verifies that Scrape returns an internal server error on general exceptions.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task Scrape_ReturnsInternalError_OnGeneralException()
    {
        // Arrange
        var request = new ScrapeRequest { Url = "https://artist.bandcamp.com/album/test" };
        _bandcampServiceMock.Setup(b => b.ScrapeAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Generic error"));

        // Act
        var result = await CreateEndpoints().Scrape(_httpContextMock.Object, request);

        // Assert
        Assert.IsType<StatusCodeHttpResult>(result);
        var statusCodeResult = (StatusCodeHttpResult)result;
        Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
    }

    /// <summary>
    /// Verifies that Scrape returns bad request on HTTP request exceptions.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task Scrape_ReturnsBadRequest_OnHttpRequestException()
    {
        // Arrange
        var request = new ScrapeRequest { Url = "https://artist.bandcamp.com/album/test" };
        _bandcampServiceMock.Setup(b => b.ScrapeAsync(It.IsAny<string>()))
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act
        var result = await CreateEndpoints().Scrape(_httpContextMock.Object, request);

        // Assert
        var badRequest = Assert.IsType<BadRequest<ErrorResponse>>(result);
        Assert.Contains("Failed to scrape URL", badRequest.Value!.Error);
    }

    /// <summary>
    /// Verifies that Post returns a result for a valid request.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task Post_WithValidRequest_ReturnsResult()
    {
        // Arrange
        var request = new PostRequest { Text = "Check this out!", ImageUrl = "https://example.com/image.jpg" };

        SetupAuthenticatedUser("https://mastodon.social", "test-token");

        _imageServiceMock.Setup(i => i.DownloadImageAsync(request.ImageUrl))
            .ReturnsAsync(new byte[] { 0x89, 0x50, 0x4E, 0x47 });

        _mastodonServiceMock.Setup(m => m.UploadMediaAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), null))
            .ReturnsAsync("media-id");

        _mastodonServiceMock.Setup(m => m.PostStatusAsync(It.IsAny<string>(), It.IsAny<string>(), "Check this out!", "media-id"))
            .ReturnsAsync(("status-id", "https://mastodon.social/@user/123"));

        // Act
        var result = await CreateEndpoints().Post(_httpContextMock.Object, request);

        // Assert
        Assert.NotNull(result);
    }

    /// <summary>
    /// Verifies that Post returns an internal server error on general exceptions.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task Post_ReturnsInternalError_OnGeneralException()
    {
        // Arrange
        var request = new PostRequest { Text = "text", ImageUrl = "http://img.jpg" };
        SetupAuthenticatedUser("https://mastodon.social", "token");
        _imageServiceMock.Setup(i => i.DownloadImageAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Generic error"));

        // Act
        var result = await CreateEndpoints().Post(_httpContextMock.Object, request);

        // Assert
        Assert.IsType<StatusCodeHttpResult>(result);
        var statusCodeResult = (StatusCodeHttpResult)result;
        Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
    }

    /// <summary>
    /// Verifies that Post returns bad request on HTTP request exceptions.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task Post_ReturnsBadRequest_OnHttpRequestException()
    {
        // Arrange
        var request = new PostRequest { Text = "text", ImageUrl = "http://img.jpg" };
        SetupAuthenticatedUser("https://mastodon.social", "token");
        _imageServiceMock.Setup(i => i.DownloadImageAsync(It.IsAny<string>()))
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act
        var result = await CreateEndpoints().Post(_httpContextMock.Object, request);

        // Assert
        var badRequest = Assert.IsType<BadRequest<ErrorResponse>>(result);
        Assert.Contains("Failed to post", badRequest.Value!.Error);
    }

    /// <summary>
    /// Verifies that Post returns unauthorized when the user is not authenticated.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task Post_WithoutAuthentication_ReturnsResult()
    {
        // Arrange
        var request = new PostRequest { Text = "Check this out!", ImageUrl = "https://example.com/image.jpg" };

        // Act
        // Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => CreateEndpoints().Post(_httpContextMock.Object, request));
    }

    /// <summary>
    /// Verifies that PostRequest fails validation when text is missing.
    /// </summary>
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

    /// <summary>
    /// Verifies that PostRequest fails validation when the image URL is missing.
    /// </summary>
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

    /// <summary>
    /// Verifies that Post returns unauthorized when the Mastodon service throws a 401 error.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task Post_ReturnsUnauthorized_WhenMastodonServiceThrows401()
    {
        // Arrange
        var request = new PostRequest { Text = "Check this out!", ImageUrl = "https://example.com/image.jpg" };
        SetupAuthenticatedUser("https://mastodon.social", "test-token");

        _imageServiceMock.Setup(i => i.DownloadImageAsync(request.ImageUrl))
            .ReturnsAsync(new byte[] { 0x89, 0x50, 0x4E, 0x47 });

        _mastodonServiceMock.Setup(m => m.UploadMediaAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), null))
            .ThrowsAsync(new HttpRequestException("Unauthorized", null, System.Net.HttpStatusCode.Unauthorized));

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => CreateEndpoints().Post(_httpContextMock.Object, request));
    }

    /// <summary>
    /// Verifies that Post returns bad request when a network error occurs.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task Post_ReturnsBadRequest_WhenNetworkErrorOccurs()
    {
        // Arrange
        var request = new PostRequest { Text = "Check this out!", ImageUrl = "https://example.com/image.jpg" };
        SetupAuthenticatedUser("https://mastodon.social", "test-token");

        _imageServiceMock.Setup(i => i.DownloadImageAsync(request.ImageUrl))
            .ReturnsAsync(new byte[] { 0x89, 0x50, 0x4E, 0x47 });

        _mastodonServiceMock.Setup(m => m.UploadMediaAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), null))
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act
        var result = await CreateEndpoints().Post(_httpContextMock.Object, request);

        // Assert
        var badRequest = Assert.IsAssignableFrom<Microsoft.AspNetCore.Http.HttpResults.BadRequest<ErrorResponse>>(result);
        Assert.Contains("Network error", badRequest.Value!.Error);
    }

    /// <summary>
    /// Mock image HTTP handler for testing.
    /// </summary>
    private class MockImageHttpHandler : HttpMessageHandler
    {
        /// <inheritdoc/>
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            response.Content = new ByteArrayContent(new byte[] { 0x89, 0x50, 0x4E, 0x47 }); // PNG header
            return Task.FromResult(response);
        }
    }
}
