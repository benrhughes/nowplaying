// Copyright (c) Ben Hughes. SPDX-License-Identifier: AGPL-3.0-or-later
namespace NowPlaying.Services;

/// <summary>
/// Manages temporary caching of composite images in memory.
/// </summary>
public interface ICompositeImageCache
{
    /// <summary>
    /// Stores a composite image in the cache and returns a unique ID.
    /// </summary>
    /// <param name="imageData">The image bytes to cache.</param>
    /// <returns>A unique ID to retrieve the cached image.</returns>
    string Store(byte[] imageData);

    /// <summary>
    /// Retrieves a composite image from the cache.
    /// </summary>
    /// <param name="cacheId">The ID returned by Store().</param>
    /// <returns>The image bytes, or null if not found or expired.</returns>
    byte[] ? Retrieve(string cacheId);

    /// <summary>
    /// Removes a composite image from the cache.
    /// </summary>
    /// <param name="cacheId">The ID to remove.</param>
    void Remove(string cacheId);
}
