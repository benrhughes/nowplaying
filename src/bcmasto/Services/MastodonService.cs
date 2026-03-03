namespace BcMasto.Services;

using System.Text.Json;
using BcMasto.Extensions;
using BcMasto.Models;

/// <summary>
/// Service for interacting with the Mastodon API.
/// </summary>
/// <param name="httpClient">The HTTP client.</param>
/// <param name="logger">The logger.</param>
public class MastodonService(HttpClient httpClient, ILogger<MastodonService> logger)
    : IMastodonService
{
    /// <inheritdoc/>
    public async Task<(string ClientId, string ClientSecret)> RegisterAppAsync(string instance, string redirectUri)
    {
        var registerData = new
        {
            client_name = AppConfig.AppName,
            redirect_uris = redirectUri,
            scopes = "write:media write:statuses",
        };

        var response = await httpClient.PostAsJsonAsync($"{instance}/api/v1/apps", registerData);

        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            logger.LogError("App registration failed: {StatusCode} {Content}", response.StatusCode, content);
            throw new HttpRequestException($"Failed to register app: {response.StatusCode}");
        }

        var data = await response.Content.ReadAsAsync<Dictionary<string, object>>();
        var clientId = data?["client_id"]?.ToString() ?? throw new InvalidOperationException("No client_id in response");
        var clientSecret = data?["client_secret"]?.ToString() ?? throw new InvalidOperationException("No client_secret in response");

        return (clientId, clientSecret);
    }

    /// <inheritdoc/>
    public async Task<string> GetAccessTokenAsync(string instance, string clientId, string clientSecret, string code, string redirectUri)
    {
        var parameters = new Dictionary<string, string>
        {
            { "client_id", clientId },
            { "client_secret", clientSecret },
            { "redirect_uri", redirectUri },
            { "grant_type", "authorization_code" },
            { "code", code },
        };

        var content = new FormUrlEncodedContent(parameters);
        var response = await httpClient.PostAsync($"{instance}/oauth/token", content);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            logger.LogError("Token exchange failed: {StatusCode} {Content}", response.StatusCode, errorContent);
            throw new HttpRequestException($"Failed to get access token: {response.StatusCode}");
        }

        var data = await response.Content.ReadAsAsync<Dictionary<string, object>>();
        var accessToken = data?["access_token"]?.ToString()
            ?? throw new InvalidOperationException("No access_token in response");

        return accessToken;
    }

    /// <inheritdoc/>
    public async Task<string> UploadMediaAsync(string instance, string accessToken, byte[] imageData, string? altText)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"{instance}/api/v1/media");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        using (var form = new MultipartFormDataContent())
        {
            form.Add(new ByteArrayContent(imageData), "file", "album.jpg");
            if (!string.IsNullOrEmpty(altText))
            {
                form.Add(new StringContent(altText), "description");
            }

            request.Content = form;
            var response = await httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                logger.LogError("Media upload failed: {StatusCode} {Content}", response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to upload media: {response.StatusCode}");
            }

            var data = await response.Content.ReadAsAsync<Dictionary<string, object>>();
            var mediaId = data?["id"]?.ToString()
                ?? throw new InvalidOperationException("No media id in response");

            return mediaId;
        }
    }

    /// <inheritdoc/>
    public async Task<(string StatusId, string Url)> PostStatusAsync(string instance, string accessToken, string text, string mediaId)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"{instance}/api/v1/statuses");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var statusRequest = new
        {
            status = text,
            media_ids = new[] { mediaId },
            visibility = "public",
        };

        request.Content = JsonContent.Create(statusRequest);
        var response = await httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            logger.LogError("Status post failed: {StatusCode} {Content}", response.StatusCode, errorContent);
            throw new HttpRequestException($"Failed to post status: {response.StatusCode}");
        }

        var data = await response.Content.ReadAsAsync<Dictionary<string, object>>();
        var statusId = data?["id"]?.ToString()
            ?? throw new InvalidOperationException("No status id in response");
        var url = data?["url"]?.ToString()
            ?? throw new InvalidOperationException("No url in response");

        return (statusId, url);
    }
}
