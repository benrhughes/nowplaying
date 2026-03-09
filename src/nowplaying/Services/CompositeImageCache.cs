// Copyright (c) Ben Hughes. SPDX-License-Identifier: AGPL-3.0-or-later
namespace NowPlaying.Services;

using Microsoft.Extensions.Caching.Memory;

/// <summary>
/// In-memory cache for temporary composite images with automatic expiration.
/// </summary>
public class CompositeImageCache : ICompositeImageCache
{
    private readonly IMemoryCache memoryCache;
    private readonly ILogger<CompositeImageCache> logger;
    private readonly TimeSpan cacheExpiration = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeImageCache"/> class.
    /// </summary>
    /// <param name="memoryCache">The memory cache.</param>
    /// <param name="logger">The logger.</param>
    public CompositeImageCache(IMemoryCache memoryCache, ILogger<CompositeImageCache> logger)
    {
        this.memoryCache = memoryCache;
        this.logger = logger;
    }

    /// <inheritdoc/>
    public string Store(byte[] imageData)
    {
        var cacheId = Guid.NewGuid().ToString();

        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(this.cacheExpiration);

        this.memoryCache.Set(cacheId, imageData, cacheOptions);

        this.logger.LogInformation("Stored composite image in cache with ID {CacheId}, expires in {Expiration} minutes", cacheId, this.cacheExpiration.TotalMinutes);
        return cacheId;
    }

    /// <inheritdoc/>
    public byte[] ? Retrieve(string cacheId)
    {
        if (this.memoryCache.TryGetValue(cacheId, out byte[] ? imageData))
        {
            return imageData;
        }

        this.logger.LogWarning("Cache entry {CacheId} not found", cacheId);
        return null;
    }

    /// <inheritdoc/>
    public void Remove(string cacheId)
    {
        this.memoryCache.Remove(cacheId);
        this.logger.LogInformation("Removed composite image from cache with ID {CacheId}", cacheId);
    }
}
