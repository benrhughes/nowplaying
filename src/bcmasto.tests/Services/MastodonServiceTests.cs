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
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<ILogger<MastodonService>> _loggerMock;
    private readonly MastodonService _service;
    private const string RedirectUri = "http://localhost:5000/auth/callback";
    private const string Instance = "https://mastodon.social";

    public MastodonServiceTests()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _loggerMock = new Mock<ILogger<MastodonService>>();
        _service = new MastodonService(_httpClientFactoryMock.Object, _loggerMock.Object, RedirectUri);
    }

    [Fact]
    public async Task RegisterAppAsync_WithValidInstance_ReturnsClientIdAndSecret()
    {
        // Arrange
        var responseData = new { client_id = "test-client-id", client_secret = "test-secret" };
        var responseJson = JsonSerializer.Serialize(responseData);
        
        var httpClient = new HttpClient(new MockHttpMessageHandler(responseJson));
        _httpClientFactoryMock.Setup(f => f.CreateClient("Default")).Returns(httpClient);

        // Act
        var (clientId, clientSecret) = await _service.RegisterAppAsync(Instance);

        // Assert
        Assert.Equal("test-client-id", clientId);
        Assert.Equal("test-secret", clientSecret);
    }

    [Fact]
    public async Task RegisterAppAsync_WithFailedResponse_ThrowsException()
    {
        // Arrange
        var httpClient = new HttpClient(new MockHttpMessageHandler("Error", System.Net.HttpStatusCode.BadRequest));
        _httpClientFactoryMock.Setup(f => f.CreateClient("Default")).Returns(httpClient);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => _service.RegisterAppAsync(Instance));
    }

    [Fact]
    public async Task RegisterAppAsync_WithMissingClientId_ThrowsException()
    {
        // Arrange
        var responseData = new { client_secret = "test-secret" };
        var responseJson = JsonSerializer.Serialize(responseData);
        
        var httpClient = new HttpClient(new MockHttpMessageHandler(responseJson));
        _httpClientFactoryMock.Setup(f => f.CreateClient("Default")).Returns(httpClient);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.RegisterAppAsync(Instance));
    }

    [Fact]
    public async Task GetAccessTokenAsync_WithValidCode_ReturnsAccessToken()
    {
        // Arrange
        var responseData = new { access_token = "test-access-token", token_type = "Bearer" };
        var responseJson = JsonSerializer.Serialize(responseData);
        
        var httpClient = new HttpClient(new MockHttpMessageHandler(responseJson));
        _httpClientFactoryMock.Setup(f => f.CreateClient("Default")).Returns(httpClient);

        // Act
        var accessToken = await _service.GetAccessTokenAsync(Instance, "client-id", "secret", "code");

        // Assert
        Assert.Equal("test-access-token", accessToken);
    }

    [Fact]
    public async Task GetAccessTokenAsync_WithFailedResponse_ThrowsException()
    {
        // Arrange
        var httpClient = new HttpClient(new MockHttpMessageHandler("Error", System.Net.HttpStatusCode.BadRequest));
        _httpClientFactoryMock.Setup(f => f.CreateClient("Default")).Returns(httpClient);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => 
            _service.GetAccessTokenAsync(Instance, "client-id", "secret", "code"));
    }

    [Fact]
    public async Task GetAccessTokenAsync_WithMissingAccessToken_ThrowsException()
    {
        // Arrange
        var responseData = new { token_type = "Bearer" };
        var responseJson = JsonSerializer.Serialize(responseData);
        
        var httpClient = new HttpClient(new MockHttpMessageHandler(responseJson));
        _httpClientFactoryMock.Setup(f => f.CreateClient("Default")).Returns(httpClient);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => 
            _service.GetAccessTokenAsync(Instance, "client-id", "secret", "code"));
    }

    [Fact]
    public async Task UploadMediaAsync_WithValidData_ReturnsMediaId()
    {
        // Arrange
        var responseData = new { id = "media-123", type = "image", url = "https://example.com/media" };
        var responseJson = JsonSerializer.Serialize(responseData);
        
        var httpClient = new HttpClient(new MockHttpMessageHandler(responseJson));
        _httpClientFactoryMock.Setup(f => f.CreateClient("Default")).Returns(httpClient);

        var imageData = new byte[] { 0x89, 0x50, 0x4E, 0x47 }; // PNG header

        // Act
        var mediaId = await _service.UploadMediaAsync(Instance, "access-token", imageData, "Alt text");

        // Assert
        Assert.Equal("media-123", mediaId);
    }

    [Fact]
    public async Task UploadMediaAsync_WithFailedResponse_ThrowsException()
    {
        // Arrange
        var httpClient = new HttpClient(new MockHttpMessageHandler("Error", System.Net.HttpStatusCode.BadRequest));
        _httpClientFactoryMock.Setup(f => f.CreateClient("Default")).Returns(httpClient);

        var imageData = new byte[] { 0x89, 0x50, 0x4E, 0x47 };

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => 
            _service.UploadMediaAsync(Instance, "access-token", imageData, "Alt text"));
    }

    [Fact]
    public async Task UploadMediaAsync_WithMissingMediaId_ThrowsException()
    {
        // Arrange
        var responseData = new { type = "image", url = "https://example.com/media" };
        var responseJson = JsonSerializer.Serialize(responseData);
        
        var httpClient = new HttpClient(new MockHttpMessageHandler(responseJson));
        _httpClientFactoryMock.Setup(f => f.CreateClient("Default")).Returns(httpClient);

        var imageData = new byte[] { 0x89, 0x50, 0x4E, 0x47 };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => 
            _service.UploadMediaAsync(Instance, "access-token", imageData, "Alt text"));
    }

    [Fact]
    public async Task PostStatusAsync_WithValidData_ReturnsStatusIdAndUrl()
    {
        // Arrange
        var responseData = new { id = "status-456", url = "https://mastodon.social/@user/123", content = "Test post" };
        var responseJson = JsonSerializer.Serialize(responseData);
        
        var httpClient = new HttpClient(new MockHttpMessageHandler(responseJson));
        _httpClientFactoryMock.Setup(f => f.CreateClient("Default")).Returns(httpClient);

        // Act
        var (statusId, url) = await _service.PostStatusAsync(Instance, "access-token", "Test post", "media-123");

        // Assert
        Assert.Equal("status-456", statusId);
        Assert.Equal("https://mastodon.social/@user/123", url);
    }

    [Fact]
    public async Task PostStatusAsync_WithFailedResponse_ThrowsException()
    {
        // Arrange
        var httpClient = new HttpClient(new MockHttpMessageHandler("Error", System.Net.HttpStatusCode.BadRequest));
        _httpClientFactoryMock.Setup(f => f.CreateClient("Default")).Returns(httpClient);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => 
            _service.PostStatusAsync(Instance, "access-token", "Test post", "media-123"));
    }

    [Fact]
    public async Task PostStatusAsync_WithMissingStatusId_ThrowsException()
    {
        // Arrange
        var responseData = new { url = "https://mastodon.social/@user/123", content = "Test post" };
        var responseJson = JsonSerializer.Serialize(responseData);
        
        var httpClient = new HttpClient(new MockHttpMessageHandler(responseJson));
        _httpClientFactoryMock.Setup(f => f.CreateClient("Default")).Returns(httpClient);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => 
            _service.PostStatusAsync(Instance, "access-token", "Test post", "media-123"));
    }

    [Fact]
    public async Task PostStatusAsync_WithMissingUrl_ThrowsException()
    {
        // Arrange
        var responseData = new { id = "status-456", content = "Test post" };
        var responseJson = JsonSerializer.Serialize(responseData);
        
        var httpClient = new HttpClient(new MockHttpMessageHandler(responseJson));
        _httpClientFactoryMock.Setup(f => f.CreateClient("Default")).Returns(httpClient);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => 
            _service.PostStatusAsync(Instance, "access-token", "Test post", "media-123"));
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
                response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            }
            return Task.FromResult(response);
        }
    }
}
