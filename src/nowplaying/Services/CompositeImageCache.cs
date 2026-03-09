// Copyright (c) Ben Hughes. SPDX-License-Identifier: AGPL-3.0-or-later
namespace NowPlaying.Services;

using Microsoft.Extensions.Caching.Memory;

/// <summary>
/// In-memory cache for temporary composite images with automatic expiration.
/// </summary>
public class CompositeImageCache : ICompositeImageCache
{
    private readonly IMemoryCache _memoryCache;

    private readonly ILogger<CompositeImageCache> _logger;

    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeImageCache"/> class.
    /// </summary>
    /// <param name="memoryCache">The memory cache.</param>
    /// <param name="logger">The logger.</param>
    public CompositeImageCache(IMemoryCache memoryCache, ILogger<CompositeImageCache> logger)
    {
        this._memoryCache = memoryCache;
        this._logger = logger;
    }

    /// <inheritdoc/>
    public string Store(byte[] imageData)
    {
        var cacheId = Guid.NewGuid().ToString();

        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(this._cacheExpiration);

        this._memoryCache.Set(cacheId, imageData, cacheOptions);

        this._logger.LogInformation("Stored composite image in cache with ID {CacheId}, expires in {Expiration} minutes", cacheId, this._cacheExpiration.TotalMinutes);
        return cacheId;
    }

    /// <inheritdoc/>
    public byte[] ? Retrieve(string cacheId)
    {
        if (this._memoryCache.TryGetValue(cacheId, out byte[] ? imageData))
        {
            return imageData;
        }

        this._logger.LogWarning("Cache entry {CacheId} not found", cacheId);
        return null;
    }

    /// <inheritdoc/>
    public void Remove(string cacheId)
    {
        this._memoryCache.Remove(cacheId);
        this._logger.LogInformation("Removed composite image from cache with ID {CacheId}", cacheId);
    }
}
