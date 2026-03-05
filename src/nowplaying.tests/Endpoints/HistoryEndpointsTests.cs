namespace NowPlaying.Tests.Endpoints;

using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
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
    private readonly DefaultHttpContext _context;

    public HistoryEndpointsTests()
    {
        _mastodonServiceMock = new Mock<IMastodonService>();
        _imageServiceMock = new Mock<IImageService>();
        _context = new DefaultHttpContext();
        
        // Setup session
        var sessionMock = new Mock<ISession>();
        var sessionStore = new Dictionary<string, byte[]>();
        
#pragma warning disable CS8601, CS8625
        sessionMock.Setup(s => s.TryGetValue(It.IsAny<string>(), out It.Ref<byte[]>.IsAny))
            .Returns((string key, out byte[] value) =>
            {
                if (sessionStore.TryGetValue(key, out var storedValue))
                {
                    value = storedValue;
                    return true;
                }
                value = null;
                return false;
            });
#pragma warning restore CS8601, CS8625
            
        sessionMock.Setup(s => s.Set(It.IsAny<string>(), It.IsAny<byte[]>()))
            .Callback<string, byte[]>((key, value) => sessionStore[key] = value);

        _context.Session = sessionMock.Object;
    }

    private void SetupAuthenticatedUser(string instance, string accessToken)
    {
        var claims = ClaimsExtensions.CreateAuthenticationClaims(instance, accessToken);
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        _context.User = principal;
    }

    [Fact]
    public async Task Search_ReturnsUnauthorized_WhenNoSession()
    {
        // Act
        var result = await HistoryEndpoints.Search(_context, DateTime.Now, DateTime.Now, _mastodonServiceMock.Object);

        // Assert
        Assert.IsType<UnauthorizedHttpResult>(result);
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
        var result = await HistoryEndpoints.Search(_context, DateTime.Now.AddDays(-1), DateTime.Now, _mastodonServiceMock.Object);

        // Assert
        // We use IValueHttpResult because we can't easily assert the generic type of Ok<List<AnonymousType>>
        var jsonResult = Assert.IsAssignableFrom<IValueHttpResult>(result);
#pragma warning disable CS8602
        Assert.NotNull(jsonResult.Value);
#pragma warning restore CS8602
    }

    [Fact]
    public async Task Composite_ReturnsBadRequest_WhenNoUrls()
    {
        var request = new CompositeRequest { ImageUrls = new List<string>() };
        var result = await HistoryEndpoints.Composite(request, _imageServiceMock.Object);
        
        var badRequest = Assert.IsType<BadRequest<ErrorResponse>>(result);
#pragma warning disable CS8602
        Assert.Equal("No images provided", badRequest.Value.Error);
#pragma warning restore CS8602
    }

    [Fact]
    public async Task Composite_ReturnsFile_WhenSuccess()
    {
        var request = new CompositeRequest { ImageUrls = new List<string> { "http://img.jpg" } };
        _imageServiceMock.Setup(x => x.GenerateCompositeAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new byte[] { 1, 2, 3 });

        var result = await HistoryEndpoints.Composite(request, _imageServiceMock.Object) ?? throw new InvalidOperationException("Result should not be null");

        var fileResult = Assert.IsType<FileContentHttpResult>(result);
        Assert.Equal("image/jpeg", fileResult.ContentType);
        Assert.Equal(new byte[] { 1, 2, 3 }, fileResult.FileContents.ToArray());
    }

    [Fact]
    public async Task PostComposite_ReturnsUnauthorized_WhenNoSession()
    {
        // Act
        var emptyImage = new FormFile(new MemoryStream(new byte[0]), 0, 0, "image", "empty.jpg");
        var unauthRequest = new PostCompositeRequest { Image = emptyImage, AltText = null, Text = string.Empty };
        var result = await HistoryEndpoints.PostComposite(_context, unauthRequest, _mastodonServiceMock.Object);

        // Assert
        Assert.IsType<UnauthorizedHttpResult>(result);
    }

    [Fact]
    public async Task PostComposite_ReturnsBadRequest_WhenNoImageFile()
    {
        // Arrange
        SetupAuthenticatedUser("https://mastodon.social", "token");
        
        var formCollection = new FormCollection(
            new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "altText", "Test alt text" },
                { "text", "Test post" }
            },
            new FormFileCollection());
        
        _context.Request.ContentType = "multipart/form-data";
        _context.Request.Form = formCollection;

        // Act
        var emptyImage = new FormFile(new MemoryStream(new byte[0]), 0, 0, "image", "empty.jpg");
        var request = new PostCompositeRequest { Image = emptyImage, AltText = null, Text = string.Empty };
        var result = await HistoryEndpoints.PostComposite(_context, request, _mastodonServiceMock.Object);

        // Assert
        var badRequest = Assert.IsType<BadRequest<ErrorResponse>>(result);
#pragma warning disable CS8602
        Assert.Equal("No image provided", badRequest.Value.Error);
#pragma warning restore CS8602
    }

    [Fact]
    public async Task PostComposite_ReturnsBadRequest_WhenNoPostText()
    {
        // Arrange
        SetupAuthenticatedUser("https://mastodon.social", "token");

        var imageStream = new MemoryStream(new byte[] { 1, 2, 3 });
        var formFile = new FormFile(imageStream, 0, imageStream.Length, "image", "test.jpg");
        
        var formCollection = new FormCollection(
            new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "altText", "Test alt text" }
            },
            new FormFileCollection { formFile });
        
        _context.Request.ContentType = "multipart/form-data";
        _context.Request.Form = formCollection;

        // Act
        var request = new PostCompositeRequest { Image = formFile, AltText = null, Text = string.Empty };
        var result = await HistoryEndpoints.PostComposite(_context, request, _mastodonServiceMock.Object);

        // Assert
        var badRequest = Assert.IsType<BadRequest<ErrorResponse>>(result);
#pragma warning disable CS8602
        Assert.Equal("No post text provided", badRequest.Value.Error);
#pragma warning restore CS8602
    }

    [Fact]
    public async Task PostComposite_ReturnsOk_WhenSuccess()
    {
        // Arrange
        SetupAuthenticatedUser("https://mastodon.social", "token");

        var imageStream = new MemoryStream(new byte[] { 1, 2, 3 });
        var formFile = new FormFile(imageStream, 0, imageStream.Length, "image", "test.jpg");
        
        var formCollection = new FormCollection(
            new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "altText", "Test alt text" },
                { "text", "Test post" }
            },
            new FormFileCollection { formFile });
        
        _context.Request.ContentType = "multipart/form-data";
        _context.Request.Form = formCollection;

        _mastodonServiceMock.Setup(x => x.UploadMediaAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string>()))
            .ReturnsAsync("media-123");

        _mastodonServiceMock.Setup(x => x.PostStatusAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(("status-456", "https://mastodon.social/@user/456"));

        // Act
        var request = new PostCompositeRequest { Image = formFile, AltText = "Test alt text", Text = "Test post" };
        var result = await HistoryEndpoints.PostComposite(_context, request, _mastodonServiceMock.Object);

        // Assert
        var okResult = Assert.IsAssignableFrom<IValueHttpResult>(result);
#pragma warning disable CS8602
        Assert.NotNull(okResult.Value);
#pragma warning restore CS8602
        
        _mastodonServiceMock.Verify(
            x => x.UploadMediaAsync("https://mastodon.social", "token", It.IsAny<byte[]>(), "Test alt text"),
            Times.Once);
        
        _mastodonServiceMock.Verify(
            x => x.PostStatusAsync("https://mastodon.social", "token", "Test post", "media-123"),
            Times.Once);
    }

    [Fact]
    public async Task PostComposite_ReturnsBadRequest_WhenUploadFails()
    {
        // Arrange
        SetupAuthenticatedUser("https://mastodon.social", "token");

        var imageStream = new MemoryStream(new byte[] { 1, 2, 3 });
        var formFile = new FormFile(imageStream, 0, imageStream.Length, "image", "test.jpg");
        
        var formCollection = new FormCollection(
            new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "altText", "Test alt text" },
                { "text", "Test post" }
            },
            new FormFileCollection { formFile });
        
        _context.Request.ContentType = "multipart/form-data";
        _context.Request.Form = formCollection;

        _mastodonServiceMock.Setup(x => x.UploadMediaAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string>()))
            .ThrowsAsync(new HttpRequestException("Upload failed"));

        // Act
        var request = new PostCompositeRequest { Image = formFile, AltText = "Test alt text", Text = "Test post" };
        var result = await HistoryEndpoints.PostComposite(_context, request, _mastodonServiceMock.Object);

        // Assert
        var badRequest = Assert.IsType<BadRequest<ErrorResponse>>(result);
#pragma warning disable CS8602
        Assert.Contains("Upload failed", badRequest.Value.Error);
#pragma warning restore CS8602
    }
}
