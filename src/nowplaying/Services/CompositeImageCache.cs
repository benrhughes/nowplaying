// Copyright (c) Ben Hughes. SPDX-License-Identifier: AGPL-3.0-or-later
namespace NowPlaying.Services;

using Microsoft.Extensions.Caching.Memory;
using NowPlaying.Models;

/// <summary>
/// In-memory cache for temporary composite images with automatic expiration.
/// </summary>
/// <param name="logger">The logger.</param>
/// <param name="appConfig">The application configuration.</param>
public class CompositeImageCache(ILogger<CompositeImageCache> logger, AppConfig appConfig) : ICompositeImageCache, IDisposable
{
    private readonly MemoryCache _memoryCache = new(new MemoryCacheOptions
    {
        SizeLimit = appConfig.CacheSizeLimitMb * 1024L * 1024L // Limit in bytes, completely isolated from global cache
    });

    private readonly ILogger<CompositeImageCache> _logger = logger;

    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(appConfig.CacheExpirationMinutes);

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
                    "Cache entry {CacheId} evicted. Reason: {Reason}. Size: {Size} bytes.",
                    key,
                    reason,
                    length);
            });

        _memoryCache.Set(cacheId, imageData, cacheOptions);

        _logger.LogInformation("Stored composite image in cache with ID {CacheId}, Size: {Size} bytes. expires in {Expiration} minutes", cacheId, imageData.Length, _cacheExpiration.TotalMinutes);

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
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _memoryCache.Dispose();
        GC.SuppressFinalize(this);
    }
}
