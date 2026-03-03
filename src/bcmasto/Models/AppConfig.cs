namespace BcMasto.Models;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Application configuration settings.
/// </summary>
public class AppConfig
{
    /// <summary>
    /// Gets the application name.
    /// </summary>
    public const string AppName = "BcMasto";

    /// <summary>
    /// Gets or sets the port number web server listens on.
    /// </summary>
    [Required]
    [Range(1, 65535)]
    public int Port { get; set; } = 4444;

    /// <summary>
    /// Gets or sets the redirect URI for OAuth.
    /// </summary>
    [Required]
    [Url]
    public string RedirectUri { get; set; } = "http://localhost:4444/auth/callback";

    /// <summary>
    /// Gets or sets the session secret key.
    /// </summary>
    [Required]
    [MinLength(16)]
    public string SessionSecret { get; set; } = "dev-secret-change-in-production";
}
