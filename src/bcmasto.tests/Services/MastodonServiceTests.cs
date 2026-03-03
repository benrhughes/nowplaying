using System.Text.Json;
using BcMasto.Extensions;
using BcMasto.Models;
using BcMasto.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BcMasto.Tests.Services;

public class MastodonServiceTests
{
    private readonly Mock<ILogger<MastodonService>> _loggerMock;
    private const string RedirectUri = "http://localhost:4444/auth/callback";
    private const string Instance = "https://mastodon.social";

    public MastodonServiceTests()
    {
        _loggerMock = new Mock<ILogger<MastodonService>>();
    }

    private MastodonService CreateService(string content, System.Net.HttpStatusCode statusCode = System.Net.HttpStatusCode.OK)
    {
        var httpClient = new HttpClient(new MockHttpMessageHandler(content, statusCode));
        return new MastodonService(httpClient, _loggerMock.Object);
    }

    [Fact]
    public async Task RegisterAppAsync_WithValidInstance_ReturnsClientIdAndSecret()
    {
        // Arrange
        var responseData = new { client_id = "test-client-id", client_secret = "test-secret" };
        var service = CreateService(JsonSerializer.Serialize(responseData));

        // Act
        var (clientId, clientSecret) = await service.RegisterAppAsync(Instance, RedirectUri);

        // Assert
        Assert.Equal("test-client-id", clientId);
        Assert.Equal("test-secret", clientSecret);
    }

    [Fact]
    public async Task RegisterAppAsync_WithFailedResponse_ThrowsException()
    {
        // Arrange
        var service = CreateService("Error", System.Net.HttpStatusCode.BadRequest);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => service.RegisterAppAsync(Instance, RedirectUri));
    }

    [Fact]
    public async Task RegisterAppAsync_WithMissingClientId_ThrowsException()
    {
        // Arrange
        var responseData = new { client_secret = "test-secret" };
        var service = CreateService(JsonSerializer.Serialize(responseData));

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.RegisterAppAsync(Instance, RedirectUri));
    }

    [Fact]
    public async Task GetAccessTokenAsync_WithValidCode_ReturnsAccessToken()
    {
        // Arrange
        var responseData = new { access_token = "test-access-token", token_type = "Bearer" };
        var service = CreateService(JsonSerializer.Serialize(responseData));

        // Act
        var accessToken = await service.GetAccessTokenAsync(Instance, "client-id", "secret", "code", RedirectUri);

        // Assert
        Assert.Equal("test-access-token", accessToken);
    }

    [Fact]
    public async Task GetAccessTokenAsync_WithFailedResponse_ThrowsException()
    {
        // Arrange
        var service = CreateService("Error", System.Net.HttpStatusCode.BadRequest);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => 
            service.GetAccessTokenAsync(Instance, "client-id", "secret", "code", RedirectUri));
    }

    [Fact]
    public async Task GetAccessTokenAsync_WithMissingAccessToken_ThrowsException()
    {
        // Arrange
        var responseData = new { token_type = "Bearer" };
        var service = CreateService(JsonSerializer.Serialize(responseData));

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => 
            service.GetAccessTokenAsync(Instance, "client-id", "secret", "code", RedirectUri));
    }

    [Fact]
    public async Task UploadMediaAsync_WithValidData_ReturnsMediaId()
    {
        // Arrange
        var responseData = new { id = "media-123", type = "image", url = "https://example.com/media" };
        var service = CreateService(JsonSerializer.Serialize(responseData));

        var imageData = new byte[] { 0x89, 0x50, 0x4E, 0x47 }; // PNG header

        // Act
        var mediaId = await service.UploadMediaAsync(Instance, "access-token", imageData, "Alt text");

        // Assert
        Assert.Equal("media-123", mediaId);
    }

    [Fact]
    public async Task UploadMediaAsync_WithFailedResponse_ThrowsException()
    {
        // Arrange
        var service = CreateService("Error", System.Net.HttpStatusCode.BadRequest);

        var imageData = new byte[] { 0x89, 0x50, 0x4E, 0x47 };

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => 
            service.UploadMediaAsync(Instance, "access-token", imageData, "Alt text"));
    }

    [Fact]
    public async Task UploadMediaAsync_WithMissingMediaId_ThrowsException()
    {
        // Arrange
        var responseData = new { type = "image", url = "https://example.com/media" };
        var service = CreateService(JsonSerializer.Serialize(responseData));

        var imageData = new byte[] { 0x89, 0x50, 0x4E, 0x47 };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => 
            service.UploadMediaAsync(Instance, "access-token", imageData, "Alt text"));
    }

    [Fact]
    public async Task PostStatusAsync_WithValidData_ReturnsStatusIdAndUrl()
    {
        // Arrange
        var responseData = new { id = "status-456", url = "https://mastodon.social/@user/123", content = "Test post" };
        var service = CreateService(JsonSerializer.Serialize(responseData));

        // Act
        var (statusId, url) = await service.PostStatusAsync(Instance, "access-token", "Test post", "media-123");

        // Assert
        Assert.Equal("status-456", statusId);
        Assert.Equal("https://mastodon.social/@user/123", url);
    }

    [Fact]
    public async Task PostStatusAsync_WithFailedResponse_ThrowsException()
    {
        // Arrange
        var service = CreateService("Error", System.Net.HttpStatusCode.BadRequest);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => 
            service.PostStatusAsync(Instance, "access-token", "Test post", "media-123"));
    }

    [Fact]
    public async Task PostStatusAsync_WithMissingStatusId_ThrowsException()
    {
        // Arrange
        var responseData = new { url = "https://mastodon.social/@user/123", content = "Test post" };
        var service = CreateService(JsonSerializer.Serialize(responseData));

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => 
            service.PostStatusAsync(Instance, "access-token", "Test post", "media-123"));
    }

    [Fact]
    public async Task PostStatusAsync_WithMissingUrl_ThrowsException()
    {
        // Arrange
        var responseData = new { id = "status-456", content = "Test post" };
        var service = CreateService(JsonSerializer.Serialize(responseData));

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => 
            service.PostStatusAsync(Instance, "access-token", "Test post", "media-123"));
    }

    private class MockHttpMessageHandler(string? content, System.Net.HttpStatusCode statusCode = System.Net.HttpStatusCode.OK) : HttpMessageHandler
    {
        private readonly string? _content = content;
        private readonly System.Net.HttpStatusCode _statusCode = statusCode;

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
