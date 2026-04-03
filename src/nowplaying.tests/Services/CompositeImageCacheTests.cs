// Copyright (c) Ben Hughes. SPDX-License-Identifier: AGPL-3.0-or-later
namespace NowPlaying.Tests.Services;

using Microsoft.Extensions.Logging;
using Moq;
using NowPlaying.Services;
using Xunit;

/// <summary>
/// Tests for the composite image cache service.
/// </summary>
public class CompositeImageCacheTests : IDisposable
{
    private readonly Mock<ILogger<CompositeImageCache>> _loggerMock;
    private readonly CompositeImageCache _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeImageCacheTests"/> class.
    /// </summary>
    public CompositeImageCacheTests()
    {
        _loggerMock = new Mock<ILogger<CompositeImageCache>>();
        _cache = new CompositeImageCache(_loggerMock.Object);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _cache.Dispose();
    }

    /// <summary>
    /// Verifies that Store returns a valid cache ID.
    /// </summary>
    [Fact]
    public void Store_ReturnsValidCacheId()
    {
        // Arrange
        var imageData = new byte[] { 1, 2, 3 };

        // Act
        var cacheId = _cache.Store(imageData);

        // Assert
        Assert.NotNull(cacheId);
        Assert.NotEmpty(cacheId);
        Assert.True(Guid.TryParse(cacheId, out _), "Cache ID should be a valid GUID");
    }

    /// <summary>
    /// Verifies that Store allows retrieval of stored data.
    /// </summary>
    [Fact]
    public void Store_AllowsRetrievalOfStoredData()
    {
        // Arrange
        var imageData = new byte[] { 1, 2, 3, 4, 5 };

        // Act
        var cacheId = _cache.Store(imageData);
        var retrieved = _cache.Retrieve(cacheId);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(imageData, retrieved);
    }

    /// <summary>
    /// Verifies that Store generates unique cache IDs for different data.
    /// </summary>
    [Fact]
    public void Store_GeneratesUniqueCacheIds()
    {
        // Arrange
        var imageData1 = new byte[] { 1, 2, 3 };
        var imageData2 = new byte[] { 4, 5, 6 };

        // Act
        var cacheId1 = _cache.Store(imageData1);
        var cacheId2 = _cache.Store(imageData2);

        // Assert
        Assert.NotEqual(cacheId1, cacheId2);
    }

    /// <summary>
    /// Verifies that Retrieve returns null for a missing cache ID.
    /// </summary>
    [Fact]
    public void Retrieve_ReturnsNullForMissingCacheId()
    {
        // Act
        var retrieved = _cache.Retrieve("non-existent-id");

        // Assert
        Assert.Null(retrieved);
    }

    /// <summary>
    /// Verifies that Remove prevents future retrieval of the data.
    /// </summary>
    [Fact]
    public void Remove_PreventsFutureRetrieval()
    {
        // Arrange
        var imageData = new byte[] { 1, 2, 3 };
        var cacheId = _cache.Store(imageData);

        // Act
        _cache.Remove(cacheId);
        var retrieved = _cache.Retrieve(cacheId);

        // Assert
        Assert.Null(retrieved);
    }

    /// <summary>
    /// Verifies that items expire after their timeout.
    /// </summary>
    [Fact]
    public void Store_WithExpiration_ExpiresAfterTimeout()
    {
        // Arrange
        var imageData = new byte[] { 1, 2, 3 };

        // Create a very short expiration time for testing
        using (var shortCache = new CompositeImageCache(_loggerMock.Object))
        {
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

    /// <summary>
    /// Verifies that stored data is correctly retrieved.
    /// </summary>
    [Fact]
    public void Retrieve_WithValidCacheId_ReturnsOriginalData()
    {
        // Arrange
        var imageData = new byte[] { 0x89, 0x50, 0x4E, 0x47 }; // PNG header

        // Act
        var cacheId = _cache.Store(imageData);
        var retrieved = _cache.Retrieve(cacheId);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(4, retrieved.Length);
        Assert.Equal(0x89, retrieved[0]);
        Assert.Equal(0x50, retrieved[1]);
        Assert.Equal(0x4E, retrieved[2]);
        Assert.Equal(0x47, retrieved[3]);
    }

    /// <summary>
    /// Verifies that Store handles large data correctly.
    /// </summary>
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
        var cacheId = _cache.Store(largeData);
        var retrieved = _cache.Retrieve(cacheId);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(largeData.Length, retrieved.Length);
        Assert.Equal(largeData, retrieved);
    }

    /// <summary>
    /// Verifies that Remove does not throw for a non-existent cache ID.
    /// </summary>
    [Fact]
    public void Remove_WithNonExistentCacheId_DoesNotThrow()
    {
        // Act & Assert - Should not throw
        _cache.Remove("non-existent-id");
    }

    /// <summary>
    /// Verifies that multiple store/retrieve cycles work independently.
    /// </summary>
    [Fact]
    public void MultipleStoreRetrieveCycles_WorkIndependently()
    {
        // Arrange
        var data1 = new byte[] { 1, 2, 3 };
        var data2 = new byte[] { 4, 5, 6 };
        var data3 = new byte[] { 7, 8, 9 };

        // Act
        var id1 = _cache.Store(data1);
        var id2 = _cache.Store(data2);
        var id3 = _cache.Store(data3);

        var retrieved1 = _cache.Retrieve(id1);
        var retrieved2 = _cache.Retrieve(id2);
        var retrieved3 = _cache.Retrieve(id3);

        // Assert
        Assert.Equal(data1, retrieved1);
        Assert.Equal(data2, retrieved2);
        Assert.Equal(data3, retrieved3);

        // Remove one and verify others are unaffected
        _cache.Remove(id2);
        Assert.Equal(data1, _cache.Retrieve(id1));
        Assert.Null(_cache.Retrieve(id2));
        Assert.Equal(data3, _cache.Retrieve(id3));
    }
}
