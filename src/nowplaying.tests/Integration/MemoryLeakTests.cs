// Copyright (c) Ben Hughes. SPDX-License-Identifier: AGPL-3.0-or-later
namespace NowPlaying.Tests.Integration;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NowPlaying.Models;
using NowPlaying.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Diagnostics;
using Xunit;

/// <summary>
/// Integration tests verifying memory constraints.
/// </summary>
public class MemoryLeakTests
{
    /// <summary>
    /// Verifies that GenerateCompositeAsync does not leak memory over successive calls.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task ImageService_GenerateComposite_DoesNotLeakMemory()
    {
        // Arrange
        using var stream = new MemoryStream();
        using (var image = new Image<Rgba32>(300, 300))
        {
            await image.SaveAsJpegAsync(stream);
        }

        var imageBytes = stream.ToArray();

        var handler = new MockHttpMessageHandler(imageBytes);
        var client = new HttpClient(handler);
        var loggerMock = new Mock<ILogger<ImageService>>();
        var appConfig = new AppConfig();
        var service = new ImageService(client, loggerMock.Object, appConfig);

        var urls = Enumerable.Range(0, 9).Select(i => $"http://example.com/{i}.jpg").ToList();

        // Warmup: run once to jit and initialize static buffers
        await service.GenerateCompositeAsync(urls);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var initialGcMemory = GC.GetTotalMemory(true);
        var initialWsMemory = Process.GetCurrentProcess().WorkingSet64;

        // Act
        for (int i = 0; i < 50; i++)
        {
            await service.GenerateCompositeAsync(urls);
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var finalGcMemory = GC.GetTotalMemory(true);
        var finalWsMemory = Process.GetCurrentProcess().WorkingSet64;

        var gcDiff = finalGcMemory - initialGcMemory;
        var wsDiff = finalWsMemory - initialWsMemory;

        // Assert
        // ImageSharp uses a pool of buffers. WorkingSet can grow due to fragmentation or lazy OS reclamation.
        // We check that growth is bounded, even if not zero.
        Assert.True(gcDiff < 20 * 1024 * 1024, $"GC Memory grew too much: {gcDiff / 1024.0 / 1024.0:F2} MB");
        Assert.True(wsDiff < 25 * 1024 * 1024, $"WorkingSet (Native) Memory grew too much: {wsDiff / 1024.0 / 1024.0:F2} MB");
    }

    /// <summary>
    /// Verifies that the combination of ImageService and CompositeImageCache does not leak memory.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task FullImageWorkflow_DoesNotLeakMemory()
    {
        // Arrange
        var imageLoggerMock = new Mock<ILogger<ImageService>>();
        var cacheLoggerMock = new Mock<ILogger<CompositeImageCache>>();

        var cache = new CompositeImageCache(cacheLoggerMock.Object);
        var random = new Random();

        // Create a custom handler that returns images of varying sizes to stress memory management
        var multiSizeHandler = new MultiSizeImageHandler();
        var client = new HttpClient(multiSizeHandler);
        var appConfig = new AppConfig();
        var imageService = new ImageService(client, imageLoggerMock.Object, appConfig);

        var urls = Enumerable.Range(0, 16).Select(i => $"http://example.com/{i}.jpg").ToList();

        // Warmup: Jit and initial pool stabilization
        for (int i = 0; i < 5; i++)
        {
            var data = await imageService.GenerateCompositeAsync(urls);
            cache.Store(data);
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var initialGcMemory = GC.GetTotalMemory(true);
        var initialWsMemory = Process.GetCurrentProcess().WorkingSet64;

        // Act - Simulate 100 cycles of search/composite
        for (int i = 0; i < 100; i++)
        {
            var data = await imageService.GenerateCompositeAsync(urls);
            cache.Store(data);

            if (i % 5 == 0)
            {
                // Allow background tasks like cache eviction and pool cleanup to breathe
                await Task.Delay(5);
                GC.Collect(1);
            }
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var finalGcMemory = GC.GetTotalMemory(true);
        var finalWsMemory = Process.GetCurrentProcess().WorkingSet64;

        var gcDiff = finalGcMemory - initialGcMemory;
        var wsDiff = finalWsMemory - initialWsMemory;

        // Assert
        // Memory should reach a steady state, not grow indefinitely.
        // Cache limit is 50MB, pooling overhead should be stable.
        Assert.True(gcDiff < 120 * 1024 * 1024, $"GC Memory grew linearly? Diff: {gcDiff / 1024.0 / 1024.0:F2} MB after 100 cycles.");
        Assert.True(wsDiff < 200 * 1024 * 1024, $"WorkingSet grew linearly? Diff: {wsDiff / 1024.0 / 1024.0:F2} MB after 100 cycles. Final WS: {finalWsMemory / 1024.0 / 1024.0:F2} MB");

        cache.Dispose();
    }

    /// <summary>
    /// Verifies that the full workflow of searching and generating composites does not leak.
    /// This test uses the service collection to simulate real-world DI behavior.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task FullServiceCycle_DoesNotLeakMemory()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = new AppConfig
        {
            Port = 0,
            RedirectUri = "http://localhost/callback",
            SessionSecret = "test-secret-long-enough-for-validation"
        };

        services.AddSingleton(config);
        services.AddLogging();
        services.AddMemoryCache();
        services.AddSingleton<ICompositeImageCache, CompositeImageCache>();

        // Mock HttpMessageHandler for all services
        var handler = new MultiSizeImageHandler();
        services.AddHttpClient<IImageService, ImageService>()
            .ConfigurePrimaryHttpMessageHandler(() => handler);
        services.AddHttpClient<IMastodonService, MastodonService>()
            .ConfigurePrimaryHttpMessageHandler(() => handler);

        var provider = services.BuildServiceProvider();
        var imageService = provider.GetRequiredService<IImageService>();
        var mastodonService = provider.GetRequiredService<IMastodonService>();
        var cache = provider.GetRequiredService<ICompositeImageCache>();

        var urls = Enumerable.Range(0, 9).Select(i => $"http://example.com/{i}.jpg").ToList();

        // Warmup
        for (int i = 0; i < 5; i++)
        {
            var data = await imageService.GenerateCompositeAsync(urls);
            cache.Store(data);
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var initialGcMemory = GC.GetTotalMemory(true);
        var initialWsMemory = Process.GetCurrentProcess().WorkingSet64;

        // Act - Simulate many cycles
        for (int i = 0; i < 50; i++)
        {
            // Simulate Mastodon search (though mocked handler returns images, MastodonService expects JSON,
            // so we might get some errors, but we care about the allocations in the service paths)
            try
            {
                // This will fail because handler returns image data not JSON, but it stresses the allocation paths
                await foreach (var dummy in mastodonService.GetTaggedPostsAsync("https://instance", "token", "userId", "tag", DateTime.UtcNow.AddDays(-1), DateTime.UtcNow))
                {
                    Assert.NotNull(dummy);
                }
            }
            catch
            {
                // Ignore expected JSON parsing errors
            }

            var data = await imageService.GenerateCompositeAsync(urls);
            cache.Store(data);

            if (i % 10 == 0)
            {
                await Task.Delay(1);
                GC.Collect(1);
            }
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var finalGcMemory = GC.GetTotalMemory(true);
        var finalWsMemory = Process.GetCurrentProcess().WorkingSet64;

        var gcDiff = finalGcMemory - initialGcMemory;
        var wsDiff = finalWsMemory - initialWsMemory;

        // Assert
        // The process-wide WorkingSet can grow due to fragmentation and the static ImageSharp pool.
        // 250MB is a safe upper bound for total growth in a 50-cycle service run.
        Assert.True(gcDiff < 120 * 1024 * 1024, $"GC Memory grew too much: {gcDiff / 1024.0 / 1024.0:F2} MB");
        Assert.True(wsDiff < 250 * 1024 * 1024, $"WorkingSet grew too much: {wsDiff / 1024.0 / 1024.0:F2} MB. Final WS: {finalWsMemory / 1024.0 / 1024.0:F2} MB");
    }

    private class MultiSizeImageHandler : HttpMessageHandler
    {
        private readonly Random _random = new();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Rotate through different image sizes, including quite large ones to stress peak memory
            int size = _random.Next(200, 2501); // 200x200 to 2500x2500 (25MB raw each)
            using var stream = new MemoryStream();
            using (var image = new Image<Rgba32>(size, size))
            {
                await image.SaveAsJpegAsync(stream, cancellationToken);
            }

            var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(stream.ToArray())
            };
            return response;
        }
    }

    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly byte[] _content;
        private readonly Random _random = new();

        public MockHttpMessageHandler(byte[] content)
        {
            _content = content;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Simulate variable response times and slightly different content to prevent overly-optimized pooling
            await Task.Delay(_random.Next(1, 5), cancellationToken);

            var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(_content)
            };
            return response;
        }
    }
}
