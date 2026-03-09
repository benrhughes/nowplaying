// Copyright (c) Ben Hughes. SPDX-License-Identifier: AGPL-3.0-or-later
namespace NowPlaying.Tests.Endpoints;

using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Moq;
using NowPlaying.Endpoints;
using NowPlaying.Extensions;
using NowPlaying.Models;
using NowPlaying.Services;
using Xunit;

public class HistoryEndpointsTests
{
    private readonly Mock<IMastodonService> _mastodonServiceMock;
    private readonly Mock<IImageService> _imageServiceMock;
    private readonly Mock<ICompositeImageCache> _cacheServiceMock;
    private readonly Mock<ILogger<HistoryEndpoints>> _loggerMock;
    private readonly DefaultHttpContext _context;

    public HistoryEndpointsTests()
    {
        _mastodonServiceMock = new Mock<IMastodonService>();
        _imageServiceMock = new Mock<IImageService>();
        _cacheServiceMock = new Mock<ICompositeImageCache>();
        _loggerMock = new Mock<ILogger<HistoryEndpoints>>();
        _context = new DefaultHttpContext();

        // Setup session
        var sessionMock = new Mock<ISession>();
        var sessionStore = new Dictionary<string, byte[]>();

        sessionMock.Setup(s => s.TryGetValue(It.IsAny<string>(), out It.Ref<byte[] ?>.IsAny))
            .Returns((string key, out byte[] ? value) =>
            {
                if (sessionStore.TryGetValue(key, out var storedValue))
                {
                    value = storedValue;
                    return true;
                }

                value = null;
                return false;
            });

        sessionMock.Setup(s => s.Set(It.IsAny<string>(), It.IsAny<byte[]>()))
            .Callback<string, byte[]>((key, value) => sessionStore[key] = value);

        _context.Session = sessionMock.Object;
    }

    private HistoryEndpoints CreateEndpoints() => new(_mastodonServiceMock.Object, _imageServiceMock.Object, _cacheServiceMock.Object, _loggerMock.Object);

    private void SetupAuthenticatedUser(string instance, string accessToken)
    {
        var claims = ClaimsExtensions.CreateAuthenticationClaims(instance, accessToken, "test-user-id");
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        _context.User = principal;
    }

    [Fact]
    public async Task Search_ReturnsUnauthorized_WhenNoSession()
    {
        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => CreateEndpoints().Search(_context, new HistorySearchRequest { Since = DateTime.Now, Until = DateTime.Now, Tag = "nowplaying" }));
    }

    [Fact]
    public async Task Search_ReturnsOk_WithPosts()
    {
        // Arrange
        SetupAuthenticatedUser("https://mastodon.social", "token");

        _mastodonServiceMock.Setup(x => x.VerifyCredentialsAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("123");

        var posts = new List<StatusMastodonResponse>
        {
            new StatusMastodonResponse("1", "url", "desc", new List<MediaResponse> { new MediaResponse("m1", "image", "img.jpg") }, DateTimeOffset.UtcNow),
        };

        _mastodonServiceMock.Setup(x => x.GetTaggedPostsAsync(It.IsAny<string>(), It.IsAny<string>(), "123", "nowplaying", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(posts);

        // Act
        var result = await CreateEndpoints().Search(_context, new HistorySearchRequest { Since = DateTime.Now.AddDays(-1), Until = DateTime.Now, Tag = "nowplaying" });

        // Assert
        // We use IValueHttpResult because we can't easily assert the generic type of Ok<List<AnonymousType>>
        var jsonResult = Assert.IsAssignableFrom<IValueHttpResult>(result);
        Assert.NotNull(jsonResult!.Value);
    }

    [Fact]
    public async Task Search_ReturnsBadRequest_WhenSearchFails()
    {
        // Arrange
        SetupAuthenticatedUser("https://mastodon.social", "token");

        _mastodonServiceMock.Setup(x => x.VerifyCredentialsAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("123");

        _mastodonServiceMock.Setup(x => x.GetTaggedPostsAsync(It.IsAny<string>(), It.IsAny<string>(), "123", "nowplaying", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ThrowsAsync(new HttpRequestException("Search failed"));

        // Act
        var result = await CreateEndpoints().Search(_context, new HistorySearchRequest { Since = DateTime.Now.AddDays(-1), Until = DateTime.Now, Tag = "nowplaying" });

        // Assert
        var badRequest = Assert.IsType<BadRequest<ErrorResponse>>(result);
        Assert.Contains("Search failed", badRequest.Value!.Error);
    }

    [Fact]
    public async Task Composite_ReturnsBadRequest_WhenNoUrls()
    {
        var request = new CompositeRequest { ImageUrls = new List<string>() };
        var result = await CreateEndpoints().Composite(request);

        var badRequest = Assert.IsType<BadRequest<ErrorResponse>>(result);
        Assert.Equal("No images provided", badRequest.Value!.Error);
    }

    [Fact]
    public async Task Composite_ReturnsOk_WithCacheId()
    {
        var request = new CompositeRequest { ImageUrls = new List<string> { "http://img.jpg" } };
        _imageServiceMock.Setup(x => x.GenerateCompositeAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new byte[] { 1, 2, 3 });

        _cacheServiceMock.Setup(x => x.Store(It.IsAny<byte[]>()))
            .Returns("cache-123");

        var result = await CreateEndpoints().Composite(request) ?? throw new InvalidOperationException("Result should not be null");

        var okResult = Assert.IsType<Ok<CompositeResponse>>(result);
        Assert.NotNull(okResult.Value);
        Assert.Equal("cache-123", okResult.Value.CacheId);
        Assert.Equal("image/jpeg", okResult.Value.ContentType);
    }

    [Fact]
    public async Task PostComposite_ReturnsUnauthorized_WhenNoSession()
    {
        // Act & Assert
        var unauthRequest = new PostCompositeRequest { CacheId = "test-cache-id", AltText = null, Text = string.Empty };
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => CreateEndpoints().PostComposite(_context, unauthRequest));
    }

    [Fact]
    public async Task PostComposite_ReturnsBadRequest_WhenNoCacheId()
    {
        // Arrange
        SetupAuthenticatedUser("https://mastodon.social", "token");

        // Act
        var request = new PostCompositeRequest { CacheId = string.Empty, AltText = null, Text = "Test post" };
        var result = await CreateEndpoints().PostComposite(_context, request);

        // Assert
        var badRequest = Assert.IsType<BadRequest<ErrorResponse>>(result);
        Assert.Equal("Cache ID is required", badRequest.Value!.Error);
    }

    [Fact]
    public async Task PostComposite_ReturnsBadRequest_WhenNoPostText()
    {
        // Arrange
        SetupAuthenticatedUser("https://mastodon.social", "token");
        _cacheServiceMock.Setup(x => x.Retrieve(It.IsAny<string>()))
            .Returns(new byte[] { 1, 2, 3 });

        // Act
        var request = new PostCompositeRequest { CacheId = "test-cache-id", AltText = "Alt text", Text = string.Empty };
        var result = await CreateEndpoints().PostComposite(_context, request);
        var badRequest = Assert.IsType<BadRequest<ErrorResponse>>(result);
        Assert.Equal("No post text provided", badRequest.Value!.Error);
    }

    [Fact]
    public async Task PostComposite_ReturnsOk_WhenSuccess()
    {
        // Arrange
        SetupAuthenticatedUser("https://mastodon.social", "token");

        var imageData = new byte[] { 1, 2, 3 };
        _cacheServiceMock.Setup(x => x.Retrieve("test-cache-id"))
            .Returns(imageData);

        _mastodonServiceMock.Setup(x => x.UploadMediaAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string>()))
            .ReturnsAsync("media-123");

        _mastodonServiceMock.Setup(x => x.PostStatusAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(("status-456", "https://mastodon.social/@user/456"));

        // Act
        var request = new PostCompositeRequest { CacheId = "test-cache-id", AltText = "Test alt text", Text = "Test post" };
        var result = await CreateEndpoints().PostComposite(_context, request);

        // Assert
        var okResult = Assert.IsAssignableFrom<IValueHttpResult>(result);
        Assert.NotNull(okResult.Value);

        _mastodonServiceMock.Verify(
            x => x.UploadMediaAsync("https://mastodon.social", "token", imageData, "Test alt text"),
            Times.Once);

        _mastodonServiceMock.Verify(
            x => x.PostStatusAsync("https://mastodon.social", "token", "Test post", "media-123"),
            Times.Once);

        _cacheServiceMock.Verify(x => x.Remove("test-cache-id"), Times.Once);
    }

    [Fact]
    public async Task PostComposite_ReturnsBadRequest_WhenUploadFails()
    {
        // Arrange
        SetupAuthenticatedUser("https://mastodon.social", "token");

        _cacheServiceMock.Setup(x => x.Retrieve("test-cache-id"))
            .Returns(new byte[] { 1, 2, 3 });

        _mastodonServiceMock.Setup(x => x.UploadMediaAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string>()))
            .ThrowsAsync(new HttpRequestException("Upload failed"));

        // Act
        var request = new PostCompositeRequest { CacheId = "test-cache-id", AltText = "Test alt text", Text = "Test post" };
        var result = await CreateEndpoints().PostComposite(_context, request);

        // Assert
        var badRequest = Assert.IsType<BadRequest<ErrorResponse>>(result);
        Assert.Contains("Upload failed", badRequest.Value!.Error);
    }

    [Fact]
    public async Task PostComposite_ReturnsUnauthorized_WhenMastodonServiceThrows401()
    {
        // Arrange
        SetupAuthenticatedUser("https://mastodon.social", "token");

        _cacheServiceMock.Setup(x => x.Retrieve("test-cache-id"))
            .Returns(new byte[] { 1, 2, 3 });

        _mastodonServiceMock.Setup(x => x.UploadMediaAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string>()))
            .ThrowsAsync(new HttpRequestException("Unauthorized", null, System.Net.HttpStatusCode.Unauthorized));

        // Act & Assert
        var request = new PostCompositeRequest { CacheId = "test-cache-id", AltText = "Test alt text", Text = "Test post" };
        await Assert.ThrowsAsync<HttpRequestException>(() => CreateEndpoints().PostComposite(_context, request));
    }

    [Fact]
    public async Task Search_ReturnsUnauthorized_WhenMastodonServiceThrows401()
    {
        // Arrange
        SetupAuthenticatedUser("https://mastodon.social", "token");

        _mastodonServiceMock.Setup(x => x.VerifyCredentialsAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new HttpRequestException("Unauthorized", null, System.Net.HttpStatusCode.Unauthorized));

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => CreateEndpoints().Search(_context, new HistorySearchRequest { Since = DateTime.Now.AddDays(-1), Until = DateTime.Now, Tag = "#nowplaying" }));
    }
}
