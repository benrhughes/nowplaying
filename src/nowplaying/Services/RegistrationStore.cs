// Copyright (c) Ben Hughes. SPDX-License-Identifier: AGPL-3.0-or-later
using System.Collections.Concurrent;
using System.Text.Json;

namespace NowPlaying.Services;

/// <summary>
/// Persistent store for registered Mastodon app credentials per instance.
/// </summary>
/// <param name="logger">The logger.</param>
/// <param name="env">The host environment.</param>
public class RegistrationStore(ILogger<RegistrationStore> logger, IHostEnvironment env)
    : IRegistrationStore
{
    private readonly string _filePath = Path.Combine(env.ContentRootPath, "registrations.json");
    private readonly ConcurrentDictionary<string, RegistrationInfo> _store = LoadStore(Path.Combine(env.ContentRootPath, "registrations.json"), logger);
    private readonly object _fileLock = new object();

    /// <summary>
    /// Adds or updates registration information for the specified instance.
    /// </summary>
    /// <param name="instance">The Mastodon instance URL.</param>
    /// <param name="clientId">The client id returned by the instance.</param>
    /// <param name="clientSecret">The client secret returned by the instance.</param>
    /// <param name="redirectUri">The redirect URI used for OAuth callbacks.</param>
    public void Add(string instance, string clientId, string clientSecret, string redirectUri)
    {
        var key = instance.TrimEnd('/');
        var info = new RegistrationInfo(clientId, clientSecret, redirectUri);
        _store[key] = info;
        Save();
        logger.LogInformation("Registered app for instance: {Instance}", instance);
    }

    /// <summary>
    /// Tries to get the registration info for the specified instance.
    /// </summary>
    /// <param name="instance">The Mastodon instance URL.</param>
    /// <param name="info">The resulting <see cref="RegistrationInfo"/> when found.</param>
    /// <returns>True if registration info exists for the instance; otherwise false.</returns>
    public bool TryGet(string instance, out RegistrationInfo? info)
    {
        if (instance == null)
        {
            info = null;
            return false;
        }

        var key = instance.TrimEnd('/');
        return _store.TryGetValue(key, out info!);
    }

    /// <summary>
    /// Returns whether the specified instance has registration information stored.
    /// </summary>
    /// <param name="instance">The Mastodon instance URL.</param>
    /// <returns>True when registration exists; otherwise false.</returns>
    public bool Has(string instance)
    {
        if (instance == null)
        {
            return false;
        }

        return _store.ContainsKey(instance.TrimEnd('/'));
    }

    private static ConcurrentDictionary<string, RegistrationInfo> LoadStore(string path, ILogger logger)
    {
        if (!File.Exists(path))
        {
            return new ConcurrentDictionary<string, RegistrationInfo>();
        }

        try
        {
            var json = File.ReadAllText(path);
            var dict = JsonSerializer.Deserialize<Dictionary<string, RegistrationInfo>>(json);
            return dict != null
                ? new ConcurrentDictionary<string, RegistrationInfo>(dict)
                : new ConcurrentDictionary<string, RegistrationInfo>();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load registration store from {Path}", path);
            return new ConcurrentDictionary<string, RegistrationInfo>();
        }
    }

    private void Save()
    {
        lock (_fileLock)
        {
            try
            {
                var json = JsonSerializer.Serialize(_store, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_filePath, json);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to save registration store to {Path}", _filePath);
            }
        }
    }
}
