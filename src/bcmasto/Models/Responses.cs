namespace BcMasto.Models;

public record ErrorResponse(string Error);

public record StatusResponse(bool Authenticated, string? Instance, bool Registered);

public record ScrapeResponse(
    string Title,
    string Artist,
    string Album,
    string? Image,
    string Description,
    string Url);

public record PostResponse(bool Success, string StatusId, string Url);

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
