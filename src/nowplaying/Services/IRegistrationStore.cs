// Copyright (c) Ben Hughes. SPDX-License-Identifier: AGPL-3.0-or-later
namespace NowPlaying.Services;

/// <summary>
/// Represents registration information for a Mastodon instance.
/// </summary>
public record RegistrationInfo(string ClientId, string ClientSecret, string RedirectUri);

/// <summary>
/// A lightweight in-memory store for application registration details per Mastodon instance.
/// </summary>
public interface IRegistrationStore
{
    /// <summary>
    /// Adds or updates registration information for the specified instance.
    /// </summary>
    /// <param name="instance">The Mastodon instance URL.</param>
    /// <param name="clientId">The client id returned by the instance.</param>
    /// <param name="clientSecret">The client secret returned by the instance.</param>
    /// <param name="redirectUri">The redirect URI used for OAuth callbacks.</param>
    void Add(string instance, string clientId, string clientSecret, string redirectUri);

    /// <summary>
    /// Tries to get the registration info for the specified instance.
    /// </summary>
    /// <param name="instance">The Mastodon instance URL.</param>
    /// <param name="info">The resulting <see cref="RegistrationInfo"/> when found.</param>
    /// <returns>True if registration info exists for the instance; otherwise false.</returns>
    bool TryGet(string instance, out RegistrationInfo? info);

    /// <summary>
    /// Returns whether the specified instance has registration information stored.
    /// </summary>
    /// <param name="instance">The Mastodon instance URL.</param>
    /// <returns>True when registration exists; otherwise false.</returns>
    bool Has(string instance);
}
