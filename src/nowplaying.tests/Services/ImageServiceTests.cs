using NowPlaying.Services;
using Moq;
using Xunit;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Microsoft.Extensions.Logging;

namespace NowPlaying.Tests.Services;

public class ImageServiceTests
{
    [Fact]
    public async Task DownloadImageAsync_ReturnsData()
    {
        // Arrange
        var content = new byte[] { 1, 2, 3 };
        var handler = new MockHttpMessageHandler(content);
        var client = new HttpClient(handler);
        var loggerMock = new Mock<ILogger<ImageService>>();
        var service = new ImageService(client, loggerMock.Object);

        // Act
        var result = await service.DownloadImageAsync("https://example.com/image.jpg");

        // Assert
        Assert.Equal(content, result);
    }

    [Fact]
    public async Task GenerateCompositeAsync_ReturnsEmpty_WhenNoUrls()
    {
        var handler = new MockHttpMessageHandler(Array.Empty<byte>());
        var client = new HttpClient(handler);
        var loggerMock = new Mock<ILogger<ImageService>>();
        var service = new ImageService(client, loggerMock.Object);

        var result = await service.GenerateCompositeAsync(new List<string>());

        Assert.Empty(result);
    }

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
        var service = new ImageService(client, loggerMock.Object);

        var urls = new List<string> { "http://example.com/1.jpg" };

        var result = await service.GenerateCompositeAsync(urls);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    private class MockHttpMessageHandler(byte[] content) : HttpMessageHandler
    {
        private readonly byte[] _content = content;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(_content)
            });
        }
    }
}
