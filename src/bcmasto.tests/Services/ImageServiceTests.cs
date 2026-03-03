using BcMasto.Services;
using Moq;
using Xunit;

namespace BcMasto.Tests.Services;

public class ImageServiceTests
{
    [Fact]
    public async Task DownloadImageAsync_ReturnsData()
    {
        // Arrange
        var content = new byte[] { 1, 2, 3 };
        var handler = new MockHttpMessageHandler(content);
        var client = new HttpClient(handler);
        var service = new ImageService(client);

        // Act
        var result = await service.DownloadImageAsync("https://example.com/image.jpg");

        // Assert
        Assert.Equal(content, result);
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
