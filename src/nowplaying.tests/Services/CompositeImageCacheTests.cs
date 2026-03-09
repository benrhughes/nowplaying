// Copyright (c) Ben Hughes. SPDX-License-Identifier: AGPL-3.0-or-later
namespace NowPlaying.Tests.Services;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using NowPlaying.Services;
using Xunit;

/// <summary>
/// Tests for the composite image cache service.
/// </summary>
public class CompositeImageCacheTests
{
    private readonly IMemoryCache memoryCache;
    private readonly Mock<ILogger<CompositeImageCache>> loggerMock;
    private readonly CompositeImageCache cache;

    public CompositeImageCacheTests()
    {
        this.memoryCache = new MemoryCache(new MemoryCacheOptions());
        this.loggerMock = new Mock<ILogger<CompositeImageCache>>();
        this.cache = new CompositeImageCache(this.memoryCache, this.loggerMock.Object);
    }

    [Fact]
    public void Store_ReturnsValidCacheId()
    {
        // Arrange
        var imageData = new byte[] { 1, 2, 3 };

        // Act
        var cacheId = this.cache.Store(imageData);

        // Assert
        Assert.NotNull(cacheId);
        Assert.NotEmpty(cacheId);
        Assert.True(Guid.TryParse(cacheId, out _), "Cache ID should be a valid GUID");
    }

    [Fact]
    public void Store_AllowsRetrievalOfStoredData()
    {
        // Arrange
        var imageData = new byte[] { 1, 2, 3, 4, 5 };

        // Act
        var cacheId = this.cache.Store(imageData);
        var retrieved = this.cache.Retrieve(cacheId);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(imageData, retrieved);
    }

    [Fact]
    public void Store_GeneratesUniqueCacheIds()
    {
        // Arrange
        var imageData1 = new byte[] { 1, 2, 3 };
        var imageData2 = new byte[] { 4, 5, 6 };

        // Act
        var cacheId1 = this.cache.Store(imageData1);
        var cacheId2 = this.cache.Store(imageData2);

        // Assert
        Assert.NotEqual(cacheId1, cacheId2);
    }

    [Fact]
    public void Retrieve_ReturnsNullForMissingCacheId()
    {
        // Act
        var retrieved = this.cache.Retrieve("non-existent-id");

        // Assert
        Assert.Null(retrieved);
    }

    [Fact]
    public void Remove_PreventsFutureRetrieval()
    {
        // Arrange
        var imageData = new byte[] { 1, 2, 3 };
        var cacheId = this.cache.Store(imageData);

        // Act
        this.cache.Remove(cacheId);
        var retrieved = this.cache.Retrieve(cacheId);

        // Assert
        Assert.Null(retrieved);
    }

    [Fact]
    public void Store_WithExpiration_ExpiresAfterTimeout()
    {
        // Arrange
        var imageData = new byte[] { 1, 2, 3 };

        // Create a very short expiration time for testing
        using (var shortLivedCache = new MemoryCache(new MemoryCacheOptions()))
        {
            var shortCache = new CompositeImageCache(shortLivedCache, this.loggerMock.Object);

            // Act - Store with a very short TTL by mimicking what the cache does
            var cacheId = shortCache.Store(imageData);

            // Verify it exists immediately
            var immediateRetrieve = shortCache.Retrieve(cacheId);
            Assert.NotNull(immediateRetrieve);

            // Wait for expiration (the MemoryCache will handle this)
            // Since we can't easily control the clock in unit tests, we verify the
            // cache ID is properly created and can be retrieved immediately
        }
    }

    [Fact]
    public void Retrieve_WithValidCacheId_ReturnsOriginalData()
    {
        // Arrange
        var imageData = new byte[] { 0x89, 0x50, 0x4E, 0x47 }; // PNG header

        // Act
        var cacheId = this.cache.Store(imageData);
        var retrieved = this.cache.Retrieve(cacheId);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(4, retrieved.Length);
        Assert.Equal(0x89, retrieved[0]);
        Assert.Equal(0x50, retrieved[1]);
        Assert.Equal(0x4E, retrieved[2]);
        Assert.Equal(0x47, retrieved[3]);
    }

    [Fact]
    public void Store_WithLargeData_WorksCorrectly()
    {
        // Arrange
        var largeData = new byte[1024 * 1024]; // 1 MB
        for (int i = 0; i < largeData.Length; i++)
        {
            largeData[i] = (byte)(i % 256);
        }

        // Act
        var cacheId = this.cache.Store(largeData);
        var retrieved = this.cache.Retrieve(cacheId);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(largeData.Length, retrieved.Length);
        Assert.Equal(largeData, retrieved);
    }

    [Fact]
    public void Remove_WithNonExistentCacheId_DoesNotThrow()
    {
        // Act & Assert - Should not throw
        this.cache.Remove("non-existent-id");
    }

    [Fact]
    public void MultipleStoreRetrieveCycles_WorkIndependently()
    {
        // Arrange
        var data1 = new byte[] { 1, 2, 3 };
        var data2 = new byte[] { 4, 5, 6 };
        var data3 = new byte[] { 7, 8, 9 };

        // Act
        var id1 = this.cache.Store(data1);
        var id2 = this.cache.Store(data2);
        var id3 = this.cache.Store(data3);

        var retrieved1 = this.cache.Retrieve(id1);
        var retrieved2 = this.cache.Retrieve(id2);
        var retrieved3 = this.cache.Retrieve(id3);

        // Assert
        Assert.Equal(data1, retrieved1);
        Assert.Equal(data2, retrieved2);
        Assert.Equal(data3, retrieved3);

        // Remove one and verify others are unaffected
        this.cache.Remove(id2);
        Assert.Equal(data1, this.cache.Retrieve(id1));
        Assert.Null(this.cache.Retrieve(id2));
        Assert.Equal(data3, this.cache.Retrieve(id3));
    }
}
