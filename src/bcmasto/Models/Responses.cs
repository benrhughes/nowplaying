namespace BcMasto.Models;

/// <summary>
/// Response returned when an error occurs.
/// </summary>
/// <param name="Error">The error message.</param>
public record ErrorResponse(string Error);

/// <summary>
/// Response containing current authentication and registration status.
/// </summary>
/// <param name="Authenticated">Whether the user is authenticated.</param>
/// <param name="Instance">The current Mastodon instance URL.</param>
/// <param name="Registered">Whether the application is registered with the instance.</param>
public record StatusResponse(bool Authenticated, string? Instance, bool Registered);

/// <summary>
/// Response containing scraped Bandcamp album data.
/// </summary>
/// <param name="Title">The full title of the album.</param>
/// <param name="Artist">The artist name.</param>
/// <param name="Album">The album name.</param>
/// <param name="Image">The album artwork URL.</param>
/// <param name="Description">The album description.</param>
/// <param name="Url">The original Bandcamp URL.</param>
public record ScrapeResponse(
    string Title,
    string Artist,
    string Album,
    string? Image,
    string Description,
    string Url);

/// <summary>
/// Response returned after successfully posting a status.
/// </summary>
/// <param name="Success">Whether the post was successful.</param>
/// <param name="StatusId">The ID of the posted status.</param>
/// <param name="Url">The URL of the posted status.</param>
public record PostResponse(bool Success, string StatusId, string Url);

/// <summary>
/// Response returned after application registration.
/// </summary>
/// <param name="Success">Whether registration was successful.</param>
/// <param name="Instance">The Mastodon instance registered with.</param>
public record RegistrationResponse(bool Success, string Instance);

// Internal models for Mastodon API
internal record AppRegistrationResponse(
    string? clientId = null,
    string? clientSecret = null,
    string? id = null,
    string? name = null)
{
    [System.Text.Json.Serialization.JsonPropertyName("client_id")]
    public string? Client_id { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("client_secret")]
    public string? Client_secret { get; set; }
}

internal record MediaResponse(string id, string? type = null, string? url = null);

internal record StatusMastodonResponse(string id, string? url = null, string? content = null);
