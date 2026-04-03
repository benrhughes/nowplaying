// Copyright (c) Ben Hughes. SPDX-License-Identifier: AGPL-3.0-or-later
using Microsoft.Extensions.Logging;
using Moq;
using NowPlaying.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace NowPlaying.Tests.Services;

/// <summary>
/// Unit tests for the <see cref="ImageService"/> class.
/// </summary>
public class ImageServiceTests
{
    /// <summary>
    /// Verifies that DownloadImageAsync returns the expected byte data.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task DownloadImageAsync_ReturnsData()
    {
        // Arrange
        var content = new byte[] { 1, 2, 3 };
        var handler = new MockHttpMessageHandler(content);
        var client = new HttpClient(handler);
        var loggerMock = new Mock<ILogger<ImageService>>();
        var appConfig = new NowPlaying.Models.AppConfig();
        var service = new ImageService(client, loggerMock.Object, appConfig);

        // Act
        var result = await service.DownloadImageAsync("https://example.com/image.jpg");

        // Assert
        Assert.Equal(content, result);
    }

    /// <summary>
    /// Verifies that GenerateCompositeAsync returns an empty result when no URLs are provided.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task GenerateCompositeAsync_ReturnsEmpty_WhenNoUrls()
    {
        var handler = new MockHttpMessageHandler(Array.Empty<byte>());
        var client = new HttpClient(handler);
        var loggerMock = new Mock<ILogger<ImageService>>();
        var appConfig = new NowPlaying.Models.AppConfig();
        var service = new ImageService(client, loggerMock.Object, appConfig);

        var result = await service.GenerateCompositeAsync(new List<string>());

        Assert.Empty(result);
    }

    /// <summary>
    /// Verifies that GenerateCompositeAsync returns an image when URLs are provided.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task GenerateCompositeAsync_ReturnsImage_WhenUrlsProvided()
    {
        // Create a real 1x1 pixel image to return
        using var stream = new MemoryStream();
        using (var image = new Image<Rgba32>(1, 1))
        {
            await image.SaveAsJpegAsync(stream);
        }

        var imageBytes = stream.ToArray();

        var handler = new MockHttpMessageHandler(imageBytes);
        var client = new HttpClient(handler);
        var loggerMock = new Mock<ILogger<ImageService>>();
        var appConfig = new NowPlaying.Models.AppConfig();
        var service = new ImageService(client, loggerMock.Object, appConfig);

        var urls = new List<string> { "http://example.com/1.jpg" };

        var result = await service.GenerateCompositeAsync(urls);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    /// <summary>
    /// Verifies that GenerateCompositeAsync skips URLs that fail to download.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task GenerateCompositeAsync_SkipsFailedDownloads()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(Array.Empty<byte>(), System.Net.HttpStatusCode.NotFound);
        var client = new HttpClient(handler);
        var loggerMock = new Mock<ILogger<ImageService>>();
        var appConfig = new NowPlaying.Models.AppConfig();
        var service = new ImageService(client, loggerMock.Object, appConfig);

        var urls = new List<string> { "https://example.com/missing.jpg" };

        // Act
        var result = await service.GenerateCompositeAsync(urls);

        // Assert
        Assert.Empty(result);
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to download")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce());
    }

    /// <summary>
    /// Verifies that DownloadImageAsync throws for an invalid URL.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task DownloadImageAsync_Throws_ForInvalidUrl()
    {
        var client = new HttpClient();
        var loggerMock = new Mock<ILogger<ImageService>>();
        var service = new ImageService(client, loggerMock.Object, new NowPlaying.Models.AppConfig());

        await Assert.ThrowsAsync<ArgumentException>(() => service.DownloadImageAsync("not-a-url"));
    }

    /// <summary>
    /// Verifies that DownloadImageAsync throws for non-HTTP URLs.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task DownloadImageAsync_Throws_ForNonHttp()
    {
        var client = new HttpClient();
        var loggerMock = new Mock<ILogger<ImageService>>();
        var service = new ImageService(client, loggerMock.Object, new NowPlaying.Models.AppConfig());

        await Assert.ThrowsAsync<ArgumentException>(() => service.DownloadImageAsync("ftp://example.com"));
    }

    /// <summary>
    /// Verifies that DownloadImageAsync throws for loopback addresses.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task DownloadImageAsync_Throws_ForLoopback()
    {
        var client = new HttpClient();
        var loggerMock = new Mock<ILogger<ImageService>>();
        var service = new ImageService(client, loggerMock.Object, new NowPlaying.Models.AppConfig());

        await Assert.ThrowsAsync<ArgumentException>(() => service.DownloadImageAsync("http://localhost"));
        await Assert.ThrowsAsync<ArgumentException>(() => service.DownloadImageAsync("http://127.0.0.1"));
    }

    /// <summary>
    /// Verifies that DownloadImageAsync throws for private IP addresses.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task DownloadImageAsync_Throws_ForPrivateIp()
    {
        var client = new HttpClient();
        var loggerMock = new Mock<ILogger<ImageService>>();
        var service = new ImageService(client, loggerMock.Object, new NowPlaying.Models.AppConfig());

        await Assert.ThrowsAsync<ArgumentException>(() => service.DownloadImageAsync("http://192.168.1.1"));
        await Assert.ThrowsAsync<ArgumentException>(() => service.DownloadImageAsync("http://10.0.0.1"));
    }

    /// <summary>
    /// Verifies that DownloadImageAsync throws for other private IP address ranges.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task DownloadImageAsync_Throws_ForOtherPrivateIps()
    {
        var client = new HttpClient();
        var loggerMock = new Mock<ILogger<ImageService>>();
        var service = new ImageService(client, loggerMock.Object, new NowPlaying.Models.AppConfig());

        await Assert.ThrowsAsync<ArgumentException>(() => service.DownloadImageAsync("http://172.16.0.1"));
        await Assert.ThrowsAsync<ArgumentException>(() => service.DownloadImageAsync("http://169.254.1.1"));
    }

    /// <summary>
    /// Verifies that DownloadImageAsync throws for IPv6 loopback address.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task DownloadImageAsync_Throws_ForIPv6Loopback()
    {
        var client = new HttpClient();
        var loggerMock = new Mock<ILogger<ImageService>>();
        var service = new ImageService(client, loggerMock.Object, new NowPlaying.Models.AppConfig());

        await Assert.ThrowsAsync<ArgumentException>(() => service.DownloadImageAsync("http://[::1]"));
    }

    /// <summary>
    /// Verifies that DownloadImageAsync throws when DNS resolution fails.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task DownloadImageAsync_Throws_WhenDnsFails()
    {
        var client = new HttpClient();
        var loggerMock = new Mock<ILogger<ImageService>>();
        var service = new ImageService(client, loggerMock.Object, new NowPlaying.Models.AppConfig());

        // A host that hopefully doesn't exist
        await Assert.ThrowsAsync<ArgumentException>(() => service.DownloadImageAsync("http://this.does.not.exist.example.invalid"));
    }

    /// <summary>
    /// Verifies that GenerateCompositeAsync skips invalid images.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task GenerateCompositeAsync_SkipsInvalidImages()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(new byte[] { 1, 2, 3 }); // Invalid image data
        var client = new HttpClient(handler);
        var loggerMock = new Mock<ILogger<ImageService>>();
        var service = new ImageService(client, loggerMock.Object, new NowPlaying.Models.AppConfig());

        var urls = new List<string> { "https://example.com/bad-image.jpg" };

        // Act
        var result = await service.GenerateCompositeAsync(urls);

        // Assert
        Assert.Empty(result);
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to download or load")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce());
    }

    /// <summary>
    /// Mock HTTP message handler for testing.
    /// </summary>
    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly byte[] _content;
        private readonly System.Net.HttpStatusCode _statusCode;

        /// <summary>
        /// Initializes a new instance of the <see cref="MockHttpMessageHandler"/> class.
        /// </summary>
        /// <param name="content">The content to return.</param>
        /// <param name="statusCode">The status code to return.</param>
        public MockHttpMessageHandler(byte[] content, System.Net.HttpStatusCode statusCode = System.Net.HttpStatusCode.OK)
        {
            _content = content;
            _statusCode = statusCode;
        }

        /// <inheritdoc/>
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(_statusCode)
            {
                Content = new ByteArrayContent(_content)
            });
        }
    }
}
