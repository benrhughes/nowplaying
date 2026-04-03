// Copyright (c) Ben Hughes. SPDX-License-Identifier: AGPL-3.0-or-later
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Moq;
using NowPlaying.Services;
using Xunit;

namespace NowPlaying.Tests.Services;

/// <summary>
/// Unit tests for the <see cref="MastodonService"/> class.
/// </summary>
public class MastodonServiceTests
{
    private readonly Mock<ILogger<MastodonService>> _loggerMock;
    private const string _redirectUri = "http://localhost:4444/auth/callback";
    private const string _instance = "https://mastodon.social";

    /// <summary>
    /// Initializes a new instance of the <see cref="MastodonServiceTests"/> class.
    /// </summary>
    public MastodonServiceTests()
    {
        _loggerMock = new Mock<ILogger<MastodonService>>();
    }

    /// <summary>
    /// Creates a new instance of the <see cref="MastodonService"/> with a mock handler.
    /// </summary>
    /// <param name="content">The JSON content to return.</param>
    /// <param name="statusCode">The HTTP status code to return.</param>
    /// <returns>A new <see cref="MastodonService"/> instance.</returns>
    private MastodonService CreateService(string content, System.Net.HttpStatusCode statusCode = System.Net.HttpStatusCode.OK)
    {
        var httpClient = new HttpClient(new MockHttpMessageHandler(content, statusCode));
        return new MastodonService(httpClient, _loggerMock.Object);
    }

    /// <summary>
    /// Verifies that RegisterAppAsync returns client ID and secret for a valid instance.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task RegisterAppAsync_WithValidInstance_ReturnsClientIdAndSecret()
    {
        // Arrange
        var responseData = new { client_id = "test-client-id", client_secret = "test-secret" };
        var service = CreateService(JsonSerializer.Serialize(responseData));

        // Act
        var (clientId, clientSecret) = await service.RegisterAppAsync(_instance, _redirectUri);

        // Assert
        Assert.Equal("test-client-id", clientId);
        Assert.Equal("test-secret", clientSecret);
    }

    /// <summary>
    /// Verifies that RegisterAppAsync throws an exception on failed response.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task RegisterAppAsync_WithFailedResponse_ThrowsException()
    {
        // Arrange
        var service = CreateService("Error", System.Net.HttpStatusCode.BadRequest);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => service.RegisterAppAsync(_instance, _redirectUri));
    }

    /// <summary>
    /// Verifies that RegisterAppAsync throws an exception when client ID is missing.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task RegisterAppAsync_WithMissingClientId_ThrowsException()
    {
        // Arrange
        var responseData = new { client_secret = "test-secret" };
        var service = CreateService(JsonSerializer.Serialize(responseData));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RegisterAppAsync(_instance, _redirectUri));
    }

    /// <summary>
    /// Verifies that GetAccessTokenAsync returns the access token for a valid code.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task GetAccessTokenAsync_WithValidCode_ReturnsAccessToken()
    {
        // Arrange
        var responseData = new { access_token = "test-access-token", token_type = "Bearer" };
        var service = CreateService(JsonSerializer.Serialize(responseData));

        // Act
        var accessToken = await service.GetAccessTokenAsync(_instance, "client-id", "secret", "code", _redirectUri);

        // Assert
        Assert.Equal("test-access-token", accessToken);
    }

    /// <summary>
    /// Verifies that GetAccessTokenAsync throws an exception on failed response.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task GetAccessTokenAsync_WithFailedResponse_ThrowsException()
    {
        // Arrange
        var service = CreateService("Error", System.Net.HttpStatusCode.BadRequest);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            service.GetAccessTokenAsync(_instance, "client-id", "secret", "code", _redirectUri));
    }

    /// <summary>
    /// Verifies that GetAccessTokenAsync throws an exception when access token is missing.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task GetAccessTokenAsync_WithMissingAccessToken_ThrowsException()
    {
        // Arrange
        var responseData = new { token_type = "Bearer" };
        var service = CreateService(JsonSerializer.Serialize(responseData));

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.GetAccessTokenAsync(_instance, "client-id", "secret", "code", _redirectUri));
    }

    /// <summary>
    /// Verifies that UploadMediaAsync returns the media ID for valid data.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task UploadMediaAsync_WithValidData_ReturnsMediaId()
    {
        // Arrange
        var responseData = new { id = "media-123", type = "image", url = "https://example.com/media" };
        var service = CreateService(JsonSerializer.Serialize(responseData));

        var imageData = new byte[] { 0x89, 0x50, 0x4E, 0x47 }; // PNG header

        // Act
        var mediaId = await service.UploadMediaAsync(_instance, "access-token", imageData, "Alt text");

        // Assert
        Assert.Equal("media-123", mediaId);
    }

    /// <summary>
    /// Verifies that UploadMediaAsync throws an exception on failed response.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task UploadMediaAsync_WithFailedResponse_ThrowsException()
    {
        // Arrange
        var service = CreateService("Error", System.Net.HttpStatusCode.BadRequest);

        var imageData = new byte[] { 0x89, 0x50, 0x4E, 0x47 };

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            service.UploadMediaAsync(_instance, "access-token", imageData, "Alt text"));
    }

    /// <summary>
    /// Verifies that UploadMediaAsync throws an exception when media ID is missing.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task UploadMediaAsync_WithMissingMediaId_ThrowsException()
    {
        // Arrange
        var responseData = new { type = "image", url = "https://example.com/media" };
        var service = CreateService(JsonSerializer.Serialize(responseData));

        var imageData = new byte[] { 0x89, 0x50, 0x4E, 0x47 };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.UploadMediaAsync(_instance, "access-token", imageData, "Alt text"));
    }

    /// <summary>
    /// Verifies that UploadMediaAsync throws an exception when alt text exceeds the limit.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task UploadMediaAsync_WithAltTextExceedingLimit_ThrowsException()
    {
        // Arrange
        var service = CreateService(JsonSerializer.Serialize(new { }));
        var imageData = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        var longAltText = new string('a', 1501); // 1501 characters, exceeds 1500 limit

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.UploadMediaAsync(_instance, "access-token", imageData, longAltText));
        Assert.Contains("1500 character limit", exception.Message);
    }

    /// <summary>
    /// Verifies that PostStatusAsync returns status ID and URL for valid data.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task PostStatusAsync_WithValidData_ReturnsStatusIdAndUrl()
    {
        // Arrange
        var responseData = new { id = "status-456", url = "https://mastodon.social/@user/123", content = "Test post" };
        var service = CreateService(JsonSerializer.Serialize(responseData));

        // Act
        var (statusId, url) = await service.PostStatusAsync(_instance, "access-token", "Test post", "media-123");

        // Assert
        Assert.Equal("status-456", statusId);
        Assert.Equal("https://mastodon.social/@user/123", url);
    }

    /// <summary>
    /// Verifies that PostStatusAsync throws an exception on failed response.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task PostStatusAsync_WithFailedResponse_ThrowsException()
    {
        // Arrange
        var service = CreateService("Error", System.Net.HttpStatusCode.BadRequest);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            service.PostStatusAsync(_instance, "access-token", "Test post", "media-123"));
    }

    /// <summary>
    /// Verifies that PostStatusAsync throws an exception when status ID is missing.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task PostStatusAsync_WithMissingStatusId_ThrowsException()
    {
        // Arrange
        var responseData = new { url = "https://mastodon.social/@user/123", content = "Test post" };
        var service = CreateService(JsonSerializer.Serialize(responseData));

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.PostStatusAsync(_instance, "access-token", "Test post", "media-123"));
    }

    /// <summary>
    /// Verifies that PostStatusAsync throws an exception when URL is missing.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task PostStatusAsync_WithMissingUrl_ThrowsException()
    {
        // Arrange
        var responseData = new { id = "status-456", content = "Test post" };
        var service = CreateService(JsonSerializer.Serialize(responseData));

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.PostStatusAsync(_instance, "access-token", "Test post", "media-123"));
    }

    /// <summary>
    /// Verifies that VerifyCredentialsAsync returns the user ID for a valid token.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task VerifyCredentialsAsync_WithValidToken_ReturnsUserId()
    {
        // Arrange
        var responseData = new { id = "user-123", username = "testuser" };
        var service = CreateService(JsonSerializer.Serialize(responseData));

        // Act
        var userId = await service.VerifyCredentialsAsync(_instance, "access-token");

        // Assert
        Assert.Equal("user-123", userId);
    }

    /// <summary>
    /// Verifies that VerifyCredentialsAsync throws an exception on failed response.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task VerifyCredentialsAsync_WithFailedResponse_ThrowsException()
    {
        // Arrange
        var service = CreateService("Error", System.Net.HttpStatusCode.Unauthorized);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            service.VerifyCredentialsAsync(_instance, "access-token"));
    }

    /// <summary>
    /// Verifies that VerifyCredentialsAsync throws an exception when user ID is missing.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task VerifyCredentialsAsync_WithMissingUserId_ThrowsException()
    {
        // Arrange
        var responseData = new { username = "testuser" };
        var service = CreateService(JsonSerializer.Serialize(responseData));

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.VerifyCredentialsAsync(_instance, "access-token"));
    }

    /// <summary>
    /// Verifies that GetTaggedPostsAsync returns posts for valid data.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task GetTaggedPostsAsync_WithValidData_ReturnsPosts()
    {
        // Arrange
        var responseData = new[]
        {
            new { id = "1", url = "url1", content = "post1", created_at = DateTimeOffset.UtcNow, media_attachments = new List<object>(), tags = new[] { new { name = "tag" } } }
        };
        var service = CreateService(JsonSerializer.Serialize(responseData));

        // Act
        var posts = new List<NowPlaying.Models.StatusMastodonResponse>();
        await foreach (var post in service.GetTaggedPostsAsync(_instance, "token", "userId", "tag", DateTime.UtcNow.AddDays(-1), DateTime.UtcNow))
        {
            posts.Add(post);
        }

        // Assert
        Assert.Single(posts);
        Assert.Equal("1", posts.First().id);
    }

    /// <summary>
    /// Verifies that GetTaggedPostsAsync throws an exception on failed response.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task GetTaggedPostsAsync_WithFailedResponse_ThrowsException()
    {
        // Arrange
        var service = CreateService("Error", System.Net.HttpStatusCode.BadRequest);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(async () =>
        {
            await foreach (var dummy in service.GetTaggedPostsAsync(_instance, "token", "userId", "tag", DateTime.UtcNow.AddDays(-1), DateTime.UtcNow))
            {
                Assert.NotNull(dummy);
            }
        });
    }

    /// <summary>
    /// Mock HTTP message handler for testing.
    /// </summary>
    /// <param name="content">The content to return.</param>
    /// <param name="statusCode">The status code to return.</param>
    private class MockHttpMessageHandler(string? content, System.Net.HttpStatusCode statusCode = System.Net.HttpStatusCode.OK) : HttpMessageHandler
    {
        private readonly string? _content = content;
        private readonly System.Net.HttpStatusCode _statusCode = statusCode;

        /// <inheritdoc/>
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(_statusCode);
            if (_content != null)
            {
                response.Content = new StringContent(_content);
                response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            }

            return Task.FromResult(response);
        }
    }
}
