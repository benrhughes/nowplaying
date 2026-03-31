// Copyright (c) Ben Hughes. SPDX-License-Identifier: AGPL-3.0-or-later
namespace NowPlaying.Models;

using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;

/// <summary>
/// Application configuration settings.
/// </summary>
public class AppConfig
{
    /// <summary>
    /// Gets the application name.
    /// </summary>
    public const string AppName = "NowPlaying";

    /// <summary>
    /// Gets the OAuth scopes required by the application.
    /// </summary>
    public const string OAuthScopes = "write:media write:statuses read:accounts read:statuses";

    /// <summary>
    /// Gets or sets the port number web server listens on.
    /// </summary>
    [Required]
    [Range(1, 65535)]
    [ConfigurationKeyName("PORT")]
    public int Port { get; set; }

    /// <summary>
    /// Gets or sets the redirect URI for OAuth.
    /// </summary>
    [Required]
    [Url]
    [ConfigurationKeyName("REDIRECT_URI")]
    public string RedirectUri { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the session secret key.
    /// </summary>
    [Required]
    [MinLength(16)]
    [ConfigurationKeyName("SESSION_SECRET")]
    public string SessionSecret { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the HTTP client timeout in seconds.
    /// </summary>
    [Range(1, 300)]
    [ConfigurationKeyName("HTTP_TIMEOUT_SECONDS")]
    public int HttpTimeoutSeconds { get; set; } = 60;
}
