// Copyright (c) Ben Hughes. SPDX-License-Identifier: AGPL-3.0-or-later
using NowPlaying.Models;
using Xunit;

namespace NowPlaying.Tests.Models;

/// <summary>
/// Unit tests for request models.
/// </summary>
public class RequestModelsTests
{
    /// <summary>
    /// Verifies that RegisterRequest correctly stores the instance URL.
    /// </summary>
    [Fact]
    public void RegisterRequest_CreatesWithInstance()
    {
        // Arrange & Act
        var request = new RegisterRequest { Instance = "mastodon.social" };

        // Assert
        Assert.Equal("mastodon.social", request.Instance);
    }

    /// <summary>
    /// Verifies that ScrapeRequest correctly stores the URL.
    /// </summary>
    [Fact]
    public void ScrapeRequest_CreatesWithUrl()
    {
        // Arrange & Act
        var request = new ScrapeRequest { Url = "https://example.bandcamp.com/album/test" };

        // Assert
        Assert.Equal("https://example.bandcamp.com/album/test", request.Url);
    }

    /// <summary>
    /// Verifies that PostRequest correctly stores required fields.
    /// </summary>
    [Fact]
    public void PostRequest_CreatesWithRequiredFields()
    {
        // Arrange & Act
        var request = new PostRequest
        {
            Text = "Test post",
            ImageUrl = "https://example.com/image.jpg",
            AltText = "Alt text"
        };

        // Assert
        Assert.Equal("Test post", request.Text);
        Assert.Equal("https://example.com/image.jpg", request.ImageUrl);
        Assert.Equal("Alt text", request.AltText);
    }

    /// <summary>
    /// Verifies that PostRequest correctly handles null alt text.
    /// </summary>
    [Fact]
    public void PostRequest_CreatesWithNullAltText()
    {
        // Arrange & Act
        var request = new PostRequest
        {
            Text = "Test post",
            ImageUrl = "https://example.com/image.jpg"
        };

        // Assert
        Assert.Equal("Test post", request.Text);
        Assert.Equal("https://example.com/image.jpg", request.ImageUrl);
        Assert.Null(request.AltText);
    }
}
