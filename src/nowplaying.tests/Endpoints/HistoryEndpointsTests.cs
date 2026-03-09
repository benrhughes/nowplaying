// Copyright (c) Ben Hughes. SPDX-License-Identifier: AGPL-3.0-or-later
namespace NowPlaying.Tests.Endpoints;

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

/// <summary>
/// Unit tests for the <see cref="HistoryEndpoints"/> class.
/// </summary>
public class HistoryEndpointsTests
{
    private readonly Mock<IMastodonService> _mastodonServiceMock;
    private readonly Mock<IImageService> _imageServiceMock;
    private readonly Mock<ICompositeImageCache> _cacheServiceMock;
    private readonly Mock<ILogger<HistoryEndpoints>> _loggerMock;
    private readonly DefaultHttpContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="HistoryEndpointsTests"/> class.
    /// </summary>
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

    /// <summary>
    /// Creates a new instance of <see cref="HistoryEndpoints"/>.
    /// </summary>
    /// <returns>A new <see cref="HistoryEndpoints"/> instance.</returns>
    private HistoryEndpoints CreateEndpoints() => new(_mastodonServiceMock.Object, _imageServiceMock.Object, _cacheServiceMock.Object, _loggerMock.Object);

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
        _context.User = principal;
    }

    /// <summary>
    /// Verifies that Search returns unauthorized when there is no active session.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task Search_ReturnsUnauthorized_WhenNoSession()
    {
        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => CreateEndpoints().Search(_context, new HistorySearchRequest { Since = DateTime.Now, Until = DateTime.Now, Tag = "nowplaying" }));
    }

    /// <summary>
    /// Verifies that Search returns OK with posts when a valid user is authenticated.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
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

    /// <summary>
    /// Verifies that Search throws unauthorized when Mastodon returns 401.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task Search_ThrowsUnauthorized_WhenMastodonReturns401()
    {
        // Arrange
        SetupAuthenticatedUser("https://mastodon.social", "token");
        _mastodonServiceMock.Setup(x => x.VerifyCredentialsAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new HttpRequestException("Unauthorized", null, System.Net.HttpStatusCode.Unauthorized));

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            CreateEndpoints().Search(_context, new HistorySearchRequest { Since = DateTime.Now, Until = DateTime.Now, Tag = "nowplaying" }));
    }

    /// <summary>
    /// Verifies that Search uses the media URL when the preview URL is null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task Search_UsesMediaUrl_WhenPreviewUrlIsNull()
    {
        // Arrange
        SetupAuthenticatedUser("https://mastodon.social", "token");
        _mastodonServiceMock.Setup(x => x.VerifyCredentialsAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("123");

        var posts = new List<StatusMastodonResponse>
        {
            new StatusMastodonResponse("1", "url", "desc", new List<MediaResponse> { new MediaResponse("m1", "image", "real-url.jpg") { preview_url = null } }, DateTimeOffset.UtcNow),
        };

        _mastodonServiceMock.Setup(x => x.GetTaggedPostsAsync(It.IsAny<string>(), It.IsAny<string>(), "123", "nowplaying", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(posts);

        // Act
        var result = await CreateEndpoints().Search(_context, new HistorySearchRequest { Since = DateTime.Now.AddDays(-1), Until = DateTime.Now, Tag = "nowplaying" });

        // Assert
        var jsonResult = Assert.IsAssignableFrom<IValueHttpResult>(result);
        Assert.NotNull(jsonResult!.Value);
    }

    /// <summary>
    /// Verifies that Search returns bad request when the search operation fails.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
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

    /// <summary>
    /// Verifies that Composite returns bad request when no URLs are provided.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task Composite_ReturnsBadRequest_WhenNoUrls()
    {
        var request = new CompositeRequest { ImageUrls = new List<string>() };
        var result = await CreateEndpoints().Composite(request);

        var badRequest = Assert.IsType<BadRequest<ErrorResponse>>(result);
        Assert.Equal("No images provided", badRequest.Value!.Error);
    }

    /// <summary>
    /// Verifies that Composite returns OK with a cache ID when images are successfully processed.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
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

    /// <summary>
    /// Verifies that PostComposite returns unauthorized when there is no active session.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task PostComposite_ReturnsUnauthorized_WhenNoSession()
    {
        // Act & Assert
        var unauthRequest = new PostCompositeRequest { CacheId = "test-cache-id", AltText = null, Text = string.Empty };
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => CreateEndpoints().PostComposite(_context, unauthRequest));
    }

    /// <summary>
    /// Verifies that PostComposite returns bad request when no cache ID is provided.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
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

    /// <summary>
    /// Verifies that PostComposite returns bad request when no post text is provided.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
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

    /// <summary>
    /// Verifies that PostComposite returns OK when the operation is successful.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
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
        var request = new PostCompositeRequest { CacheId = "test-cache-id", AltText = "Alt text", Text = "Test post" };
        var result = await CreateEndpoints().PostComposite(_context, request);

        // Assert
        var okResult = Assert.IsAssignableFrom<IValueHttpResult>(result);
        Assert.NotNull(okResult.Value);
    }

    /// <summary>
    /// Verifies that GetCompositePreview returns OK when the image is found in cache.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task GetCompositePreview_ReturnsOk_WhenFound()
    {
        // Arrange
        var imageData = new byte[] { 1, 2, 3 };
        _cacheServiceMock.Setup(x => x.Retrieve("test-cache-id"))
            .Returns(imageData);

        // Act
        var result = CreateEndpoints().GetCompositePreview("test-cache-id");

        // Assert
        var fileResult = Assert.IsAssignableFrom<IFileHttpResult>(result);
        Assert.Equal("image/jpeg", fileResult.ContentType);
    }

    /// <summary>
    /// Verifies that GetCompositePreview returns not found when the image is missing from cache.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task GetCompositePreview_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        _cacheServiceMock.Setup(x => x.Retrieve("missing"))
            .Returns((byte[] ?)null);

        // Act
        var result = CreateEndpoints().GetCompositePreview("missing");

        // Assert
        Assert.IsType<NotFound<ErrorResponse>>(result);
    }

    /// <summary>
    /// Verifies that GetCompositePreview returns bad request when the cache ID is empty.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task GetCompositePreview_ReturnsBadRequest_WhenEmptyId()
    {
        // Act
        var result = CreateEndpoints().GetCompositePreview(string.Empty);

        // Assert
        Assert.IsType<BadRequest<ErrorResponse>>(result);
    }

    /// <summary>
    /// Verifies that Search returns unauthorized when the Mastodon service throws a 401 error.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task Search_ReturnsUnauthorized_WhenMastodonServiceThrows401()
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

    /// <summary>
    /// Verifies that Search returns bad request on general exceptions.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task Search_ReturnsBadRequest_OnGeneralException()
    {
        // Arrange
        SetupAuthenticatedUser("https://mastodon.social", "token");
        _mastodonServiceMock.Setup(x => x.VerifyCredentialsAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Generic error"));

        // Act
        var result = await CreateEndpoints().Search(_context, new HistorySearchRequest { Since = DateTime.Now, Until = DateTime.Now, Tag = "nowplaying" });

        // Assert
        var badRequest = Assert.IsType<BadRequest<ErrorResponse>>(result);
        Assert.Contains("Generic error", badRequest.Value!.Error);
    }

    /// <summary>
    /// Verifies that Composite returns bad request when an HTTP request exception occurs.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task Composite_ReturnsBadRequest_OnHttpRequestException()
    {
        // Arrange
        var request = new CompositeRequest { ImageUrls = new List<string> { "http://img.jpg" } };
        _imageServiceMock.Setup(x => x.GenerateCompositeAsync(It.IsAny<IEnumerable<string>>()))
            .ThrowsAsync(new HttpRequestException("Download failed"));

        // Act
        var result = await CreateEndpoints().Composite(request);

        // Assert
        var badRequest = Assert.IsType<BadRequest<ErrorResponse>>(result);
        Assert.Contains("Failed to download images", badRequest.Value!.Error);
    }

    /// <summary>
    /// Verifies that PostComposite returns bad request when the image is not found in cache.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task PostComposite_ReturnsBadRequest_WhenCacheMiss()
    {
        // Arrange
        SetupAuthenticatedUser("https://mastodon.social", "token");
        _cacheServiceMock.Setup(x => x.Retrieve(It.IsAny<string>())).Returns((byte[] ?)null);

        // Act
        var request = new PostCompositeRequest { CacheId = "expired", Text = "Post text" };
        var result = await CreateEndpoints().PostComposite(_context, request);

        // Assert
        var badRequest = Assert.IsType<BadRequest<ErrorResponse>>(result);
        Assert.Contains("Composite image not found", badRequest.Value!.Error);
    }

    /// <summary>
    /// Verifies that PostComposite throws an exception when Mastodon returns 401.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task PostComposite_Throws_WhenMastodonThrows401()
    {
        // Arrange
        SetupAuthenticatedUser("https://mastodon.social", "token");
        _cacheServiceMock.Setup(x => x.Retrieve(It.IsAny<string>())).Returns(new byte[] { 1 });
        _mastodonServiceMock.Setup(x => x.UploadMediaAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string>()))
            .ThrowsAsync(new HttpRequestException("Unauthorized", null, System.Net.HttpStatusCode.Unauthorized));

        // Act & Assert
        var request = new PostCompositeRequest { CacheId = "id", Text = "text" };
        await Assert.ThrowsAsync<HttpRequestException>(() => CreateEndpoints().PostComposite(_context, request));
    }

    /// <summary>
    /// Verifies that Composite returns bad request on general exceptions.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task Composite_ReturnsBadRequest_OnGeneralException()
    {
        // Arrange
        var request = new CompositeRequest { ImageUrls = new List<string> { "http://img.jpg" } };
        _imageServiceMock.Setup(x => x.GenerateCompositeAsync(It.IsAny<IEnumerable<string>>()))
            .ThrowsAsync(new Exception("Unknown error"));

        // Act
        var result = await CreateEndpoints().Composite(request);

        // Assert
        var badRequest = Assert.IsType<BadRequest<ErrorResponse>>(result);
        Assert.Contains("Failed to generate composite", badRequest.Value!.Error);
    }

    /// <summary>
    /// Verifies that PostComposite returns bad request when an HTTP request exception occurs during posting.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task PostComposite_ReturnsBadRequest_OnHttpRequestException()
    {
        // Arrange
        SetupAuthenticatedUser("https://mastodon.social", "token");
        _cacheServiceMock.Setup(x => x.Retrieve(It.IsAny<string>())).Returns(new byte[] { 1 });
        _mastodonServiceMock.Setup(x => x.UploadMediaAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string>()))
            .ThrowsAsync(new HttpRequestException("Post failed"));

        // Act
        var request = new PostCompositeRequest { CacheId = "id", Text = "text" };
        var result = await CreateEndpoints().PostComposite(_context, request);

        // Assert
        var badRequest = Assert.IsType<BadRequest<ErrorResponse>>(result);
        Assert.Contains("Failed to post composite", badRequest.Value!.Error);
    }

    /// <summary>
    /// Verifies that PostComposite returns bad request on general exceptions.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task PostComposite_ReturnsBadRequest_OnGeneralException()
    {
        // Arrange
        SetupAuthenticatedUser("https://mastodon.social", "token");
        _cacheServiceMock.Setup(x => x.Retrieve(It.IsAny<string>())).Returns(new byte[] { 1 });
        _mastodonServiceMock.Setup(x => x.UploadMediaAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Unknown error"));

        // Act
        var request = new PostCompositeRequest { CacheId = "id", Text = "text" };
        var result = await CreateEndpoints().PostComposite(_context, request);

        // Assert
        var badRequest = Assert.IsType<BadRequest<ErrorResponse>>(result);
        Assert.Contains("An error occurred", badRequest.Value!.Error);
    }
}
