using System.Collections.Concurrent;

namespace NowPlaying.Services;

/// <summary>
/// In-memory store for registered Mastodon app credentials per instance.
/// </summary>
/// <param name="logger">The logger.</param>
public class RegistrationStore(ILogger<RegistrationStore> logger)
    : IRegistrationStore
{
    private readonly ConcurrentDictionary<string, RegistrationInfo> _store = new ConcurrentDictionary<string, RegistrationInfo>();

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
}
