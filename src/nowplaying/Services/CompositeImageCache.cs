// Copyright (c) Ben Hughes. SPDX-License-Identifier: AGPL-3.0-or-later
namespace NowPlaying.Services;

using Microsoft.Extensions.Caching.Memory;

/// <summary>
/// In-memory cache for temporary composite images with automatic expiration.
/// </summary>
public class CompositeImageCache : ICompositeImageCache, IDisposable
{
    private readonly MemoryCache _memoryCache;

    private readonly ILogger<CompositeImageCache> _logger;

    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeImageCache"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public CompositeImageCache(ILogger<CompositeImageCache> logger)
    {
        _memoryCache = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = 50 * 1024 * 1024 // 50 MB limit, completely isolated from global cache
        });
        _logger = logger;
    }

    /// <inheritdoc/>
    public string Store(byte[] imageData)
    {
        var cacheId = Guid.NewGuid().ToString();

        var cacheOptions = new MemoryCacheEntryOptions()
            .SetSize(imageData.Length)
            .SetAbsoluteExpiration(_cacheExpiration)
            .RegisterPostEvictionCallback((key, value, reason, state) =>
            {
                var length = (value as byte[])?.Length ?? 0;
                _logger.LogInformation(
                    "Memory after cache entry {CacheId} evicted. Reason: {Reason}. Size: {Size} bytes. GC={GC} bytes, WS={WS} bytes",
                    key,
                    reason,
                    length,
                    GC.GetTotalMemory(false),
                    System.Diagnostics.Process.GetCurrentProcess().WorkingSet64);
            });

        _memoryCache.Set(cacheId, imageData, cacheOptions);

        _logger.LogInformation("Stored composite image in cache with ID {CacheId}, Size: {Size} bytes. expires in {Expiration} minutes", cacheId, imageData.Length, _cacheExpiration.TotalMinutes);
        _logger.LogInformation(
            "Memory after cache Store: GC={GC} bytes, WS={WS} bytes, Gen0={Gen0}, Gen1={Gen1}, Gen2={Gen2}",
            GC.GetTotalMemory(false),
            System.Diagnostics.Process.GetCurrentProcess().WorkingSet64,
            GC.CollectionCount(0),
            GC.CollectionCount(1),
            GC.CollectionCount(2));

        return cacheId;
    }

    /// <inheritdoc/>
    public byte[] ? Retrieve(string cacheId)
    {
        if (_memoryCache.TryGetValue(cacheId, out byte[] ? imageData))
        {
            return imageData;
        }

        _logger.LogWarning("Cache entry {CacheId} not found", cacheId);
        return null;
    }

    /// <inheritdoc/>
    public void Remove(string cacheId)
    {
        _memoryCache.Remove(cacheId);
        _logger.LogInformation("Removed composite image from cache with ID {CacheId}", cacheId);
        _logger.LogInformation("Memory after cache Remove: GC={GC} bytes, WS={WS} bytes", GC.GetTotalMemory(false), System.Diagnostics.Process.GetCurrentProcess().WorkingSet64);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _memoryCache.Dispose();
        GC.SuppressFinalize(this);
    }
}
