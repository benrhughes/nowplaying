using BcMasto.Models;
using Xunit;

namespace BcMasto.Tests.Models;

public class RequestModelsTests
{
    [Fact]
    public void RegisterRequest_CreatesWithInstance()
    {
        // Arrange & Act
        var request = new RegisterRequest("mastodon.social");

        // Assert
        Assert.Equal("mastodon.social", request.Instance);
    }

    [Fact]
    public void ScrapeRequest_CreatesWithUrl()
    {
        // Arrange & Act
        var request = new ScrapeRequest("https://example.bandcamp.com/album/test");

        // Assert
        Assert.Equal("https://example.bandcamp.com/album/test", request.Url);
    }

    [Fact]
    public void PostRequest_CreatesWithRequiredFields()
    {
        // Arrange & Act
        var request = new PostRequest("Test post", "https://example.com/image.jpg", "Alt text");

        // Assert
        Assert.Equal("Test post", request.Text);
        Assert.Equal("https://example.com/image.jpg", request.ImageUrl);
        Assert.Equal("Alt text", request.AltText);
    }

    [Fact]
    public void PostRequest_CreatesWithNullAltText()
    {
        // Arrange & Act
        var request = new PostRequest("Test post", "https://example.com/image.jpg");

        // Assert
        Assert.Equal("Test post", request.Text);
        Assert.Equal("https://example.com/image.jpg", request.ImageUrl);
        Assert.Null(request.AltText);
    }
}
