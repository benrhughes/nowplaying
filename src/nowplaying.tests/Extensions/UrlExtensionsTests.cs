// Copyright (c) Ben Hughes. SPDX-License-Identifier: AGPL-3.0-or-later
namespace NowPlaying.Tests.Extensions;

using NowPlaying.Extensions;
using Xunit;

/// <summary>
/// Tests for <see cref="UrlExtensions"/> normalization methods.
/// </summary>
public class UrlExtensionsTests
{
    /// <summary>
    /// Verifies that a URL with https:// is normalized correctly without changes.
    /// </summary>
    [Fact]
    public void NormalizeInstance_WithHttpsUrl_ReturnsUrlUnchanged()
    {
        // Arrange
        const string url = "https://mastodon.social";

        // Act
        var result = url.NormalizeInstance();

        // Assert
        Assert.Equal("https://mastodon.social", result);
    }

    /// <summary>
    /// Verifies that a URL with http:// is normalized correctly.
    /// </summary>
    [Fact]
    public void NormalizeInstance_WithHttpUrl_ReturnsHttpsUrl()
    {
        // Arrange
        const string url = "http://mastodon.social";

        // Act
        var result = url.NormalizeInstance();

        // Assert
        Assert.Equal("https://mastodon.social", result);
    }

    /// <summary>
    /// Verifies that a URL without protocol is normalized with https:// added.
    /// </summary>
    [Fact]
    public void NormalizeInstance_WithoutProtocol_AddsHttps()
    {
        // Arrange
        const string url = "mastodon.social";

        // Act
        var result = url.NormalizeInstance();

        // Assert
        Assert.Equal("https://mastodon.social", result);
    }

    /// <summary>
    /// Verifies that a URL with trailing slash is removed.
    /// </summary>
    [Fact]
    public void NormalizeInstance_WithTrailingSlash_RemovesSlash()
    {
        // Arrange
        const string url = "https://mastodon.social/";

        // Act
        var result = url.NormalizeInstance();

        // Assert
        Assert.Equal("https://mastodon.social", result);
    }

    /// <summary>
    /// Verifies that a URL with multiple trailing slashes is normalized.
    /// </summary>
    [Fact]
    public void NormalizeInstance_WithMultipleTrailingSlashes_RemovesAllSlashes()
    {
        // Arrange
        const string url = "https://mastodon.social///";

        // Act
        var result = url.NormalizeInstance();

        // Assert
        Assert.Equal("https://mastodon.social", result);
    }

    /// <summary>
    /// Verifies that whitespace is trimmed from URLs.
    /// </summary>
    [Fact]
    public void NormalizeInstance_WithWhitespace_TrimsWhitespace()
    {
        // Arrange
        const string url = "  https://mastodon.social  ";

        // Act
        var result = url.NormalizeInstance();

        // Assert
        Assert.Equal("https://mastodon.social", result);
    }

    /// <summary>
    /// Verifies that a URL with subdomain is normalized correctly.
    /// </summary>
    [Fact]
    public void NormalizeInstance_WithSubdomain_PreservesSubdomain()
    {
        // Arrange
        const string url = "custom.mastodon.social";

        // Act
        var result = url.NormalizeInstance();

        // Assert
        Assert.Equal("https://custom.mastodon.social", result);
    }

    /// <summary>
    /// Verifies that a URL with port number is normalized correctly.
    /// </summary>
    [Fact]
    public void NormalizeInstance_WithPort_PreservesPort()
    {
        // Arrange
        const string url = "https://localhost:3000";

        // Act
        var result = url.NormalizeInstance();

        // Assert
        Assert.Equal("https://localhost:3000", result);
    }

    /// <summary>
    /// Verifies that an empty string throws ArgumentException.
    /// </summary>
    [Fact]
    public void NormalizeInstance_WithEmptyString_ThrowsArgumentException()
    {
        // Arrange
        const string url = "";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => url.NormalizeInstance());
        Assert.Contains("Instance URL cannot be null or empty", exception.Message);
    }

    /// <summary>
    /// Verifies that null throws ArgumentException.
    /// </summary>
    [Fact]
    public void NormalizeInstance_WithNull_ThrowsArgumentException()
    {
        // Arrange
        string? url = null;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => url!.NormalizeInstance());
        Assert.Contains("Instance URL cannot be null or empty", exception.Message);
    }

    /// <summary>
    /// Verifies that an invalid URL format throws ArgumentException.
    /// </summary>
    /// <param name="url">The invalid URL to test.</param>
    [Theory]
    [InlineData("ht!tp://invalid")]
    [InlineData("https://")]
    [InlineData("://malformed")]
    public void NormalizeInstance_WithInvalidFormat_ThrowsArgumentException(string url)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => url.NormalizeInstance());
        Assert.Contains("Invalid instance URL format", exception.Message);
    }

    /// <summary>
    /// Verifies that combined whitespace, protocol, and trailing slash are handled.
    /// </summary>
    [Fact]
    public void NormalizeInstance_WithComplexInput_NormalizesAll()
    {
        // Arrange
        const string url = "  mastodon.social/  ";

        // Act
        var result = url.NormalizeInstance();

        // Assert
        Assert.Equal("https://mastodon.social", result);
    }
}
