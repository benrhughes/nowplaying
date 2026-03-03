namespace BcMasto.Services;

/// <summary>
/// Service for interacting with the Mastodon API.
/// </summary>
public interface IMastodonService
{
    /// <summary>
    /// Registers the app with a Mastodon instance.
    /// </summary>
    /// <param name="instance">The instance URL.</param>
    /// <param name="redirectUri">The redirect URI.</param>
    /// <returns>A tuple containing ClientId and ClientSecret.</returns>
    Task<(string ClientId, string ClientSecret)> RegisterAppAsync(string instance, string redirectUri);

    /// <summary>
    /// Exchanges an auth code for an access token.
    /// </summary>
    /// <param name="instance">The instance URL.</param>
    /// <param name="clientId">The client ID.</param>
    /// <param name="clientSecret">The client secret.</param>
    /// <param name="code">The auth code.</param>
    /// <param name="redirectUri">The redirect URI.</param>
    /// <returns>The access token.</returns>
    Task<string> GetAccessTokenAsync(string instance, string clientId, string clientSecret, string code, string redirectUri);

    /// <summary>
    /// Uploads an image to Mastodon.
    /// </summary>
    /// <param name="instance">The instance URL.</param>
    /// <param name="accessToken">The user access token.</param>
    /// <param name="imageData">The image data bytes.</param>
    /// <param name="altText">Optional alt text.</param>
    /// <returns>The media ID.</returns>
    Task<string> UploadMediaAsync(string instance, string accessToken, byte[] imageData, string? altText);

    /// <summary>
    /// Posts a status with media to Mastodon.
    /// </summary>
    /// <param name="instance">The instance URL.</param>
    /// <param name="accessToken">The user access token.</param>
    /// <param name="text">The status text.</param>
    /// <param name="mediaId">The uploaded media ID.</param>
    /// <returns>A tuple containing StatusId and URL.</returns>
    Task<(string StatusId, string Url)> PostStatusAsync(string instance, string accessToken, string text, string mediaId);
}
