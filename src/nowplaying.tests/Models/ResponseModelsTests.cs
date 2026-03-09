// Copyright (c) Ben Hughes. SPDX-License-Identifier: AGPL-3.0-or-later
using NowPlaying.Models;
using Xunit;

namespace NowPlaying.Tests.Models;

/// <summary>
/// Unit tests for response models.
/// </summary>
public class ResponseModelsTests
{
    /// <summary>
    /// Verifies that ErrorResponse correctly stores the error message.
    /// </summary>
    [Fact]
    public void ErrorResponse_CreatesWithError()
    {
        // Arrange & Act
        var response = new ErrorResponse("Test error");

        // Assert
        Assert.Equal("Test error", response.Error);
    }

    /// <summary>
    /// Verifies that StatusResponse correctly stores all properties.
    /// </summary>
    [Fact]
    public void StatusResponse_CreatesWithAllProperties()
    {
        // Arrange & Act
        var response = new StatusResponse(true, "mastodon.social", false);

        // Assert
        Assert.True(response.Authenticated);
        Assert.Equal("mastodon.social", response.Instance);
        Assert.False(response.Registered);
    }

    /// <summary>
    /// Verifies that ScrapeResponse correctly stores all properties.
    /// </summary>
    [Fact]
    public void ScrapeResponse_CreatesWithAllProperties()
    {
        // Arrange & Act
        var response = new ScrapeResponse(
            Title: "Test Album – Test Artist",
            Artist: "Test Artist",
            Album: "Test Album",
            Image: "https://example.com/image.jpg",
            Description: "A test album",
            Url: "https://example.bandcamp.com/album/test");

        // Assert
        Assert.Equal("Test Album – Test Artist", response.Title);
        Assert.Equal("Test Artist", response.Artist);
        Assert.Equal("Test Album", response.Album);
        Assert.Equal("https://example.com/image.jpg", response.Image);
        Assert.Equal("A test album", response.Description);
        Assert.Equal("https://example.bandcamp.com/album/test", response.Url);
    }

    /// <summary>
    /// Verifies that PostResponse correctly stores all properties.
    /// </summary>
    [Fact]
    public void PostResponse_CreatesWithAllProperties()
    {
        // Arrange & Act
        var response = new PostResponse(true, "status-123", "https://mastodon.social/@user/123");

        // Assert
        Assert.True(response.Success);
        Assert.Equal("status-123", response.StatusId);
        Assert.Equal("https://mastodon.social/@user/123", response.Url);
    }

    /// <summary>
    /// Verifies that RegistrationResponse correctly stores success status and instance URL.
    /// </summary>
    [Fact]
    public void RegistrationResponse_CreatesWithSuccessAndInstance()
    {
        // Arrange & Act
        var response = new RegistrationResponse(true, "mastodon.social");

        // Assert
        Assert.True(response.Success);
        Assert.Equal("mastodon.social", response.Instance);
    }
}
