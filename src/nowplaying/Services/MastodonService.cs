namespace NowPlaying.Services;

using System.Net;
using System.Text.Json;
using NowPlaying.Extensions;
using NowPlaying.Models;

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
        instance = instance.NormalizeInstance();
        var registerData = new
        {
            client_name = AppConfig.AppName,
            redirect_uris = redirectUri,
            scopes = AppConfig.OAuthScopes
        };

        var response = await httpClient.PostAsJsonAsync($"{instance}/api/v1/apps", registerData);

        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            logger.LogError("App registration failed: {StatusCode} {Content}", response.StatusCode, content);
            throw new HttpRequestException($"Failed to register app: {response.StatusCode}", null, response.StatusCode);
        }

        var data = await response.Content.ReadAsAsync<Dictionary<string, object>>();
        var clientId = data?["client_id"]?.ToString() ?? throw new InvalidOperationException("No client_id in response");
        var clientSecret = data?["client_secret"]?.ToString() ?? throw new InvalidOperationException("No client_secret in response");

        return (clientId, clientSecret);
    }

    /// <inheritdoc/>
    public async Task<string> GetAccessTokenAsync(
        string instance,
        string clientId,
        string clientSecret,
        string code,
        string redirectUri)
    {
        instance = instance.NormalizeInstance();
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
            throw new HttpRequestException($"Failed to get access token: {response.StatusCode}", null, response.StatusCode);
        }

        var data = await response.Content.ReadAsAsync<Dictionary<string, object>>();
        var accessToken = data?["access_token"]?.ToString()
            ?? throw new InvalidOperationException("No access_token in response");

        return accessToken;
    }

    /// <inheritdoc/>
    public async Task<string> VerifyCredentialsAsync(string instance, string accessToken)
    {
        instance = instance.NormalizeInstance();
        accessToken = accessToken?.Trim() ?? throw new ArgumentException("Access token cannot be null or empty", nameof(accessToken));
        var request = new HttpRequestMessage(HttpMethod.Get, $"{instance}/api/v1/accounts/verify_credentials");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var response = await httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            logger.LogError("Verify credentials failed: {StatusCode} {Content}", response.StatusCode, errorContent);
            throw new HttpRequestException($"Failed to verify credentials: {response.StatusCode}", null, response.StatusCode);
        }

        var data = await response.Content.ReadAsAsync<Dictionary<string, object>>();
        return data?["id"]?.ToString() ?? throw new InvalidOperationException("No user id in response");
    }

    /// <inheritdoc/>
    public async Task<string> UploadMediaAsync(string instance, string accessToken, byte[] imageData, string? altText)
    {
        instance = instance.NormalizeInstance();
        accessToken = accessToken?.Trim() ?? throw new ArgumentException("Access token cannot be null or empty", nameof(accessToken));

        if (!string.IsNullOrEmpty(altText) && altText.Length > 1500)
        {
            throw new ArgumentException($"Alt text exceeds the 1500 character limit (current length: {altText.Length})", nameof(altText));
        }

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
                throw new HttpRequestException($"Failed to upload media: {response.StatusCode}", null, response.StatusCode);
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
        instance = instance.NormalizeInstance();
        accessToken = accessToken?.Trim() ?? throw new ArgumentException("Access token cannot be null or empty", nameof(accessToken));
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
            throw new HttpRequestException($"Failed to post status: {response.StatusCode}", null, response.StatusCode);
        }

        var data = await response.Content.ReadAsAsync<Dictionary<string, object>>();
        var statusId = data?["id"]?.ToString()
            ?? throw new InvalidOperationException("No status id in response");
        var url = data?["url"]?.ToString()
            ?? throw new InvalidOperationException("No url in response");

        return (statusId, url);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<StatusMastodonResponse>> GetTaggedPostsAsync(string instance, string accessToken, string userId, string tag, DateTime since, DateTime until)
    {
        instance = instance.NormalizeInstance();
        accessToken = accessToken?.Trim() ?? throw new ArgumentException("Access token cannot be null or empty", nameof(accessToken));
        var statuses = new List<StatusMastodonResponse>();
        string? maxId = null;
        bool hasMore = true;

        while (hasMore)
        {
            // Use the API-side 'tagged' parameter for server-side hashtag filtering.
            // The Mastodon API provides this parameter on /accounts/:id/statuses endpoint,
            // which is more efficient than client-side filtering.
            // Date range filtering is still done client-side as the API doesn't support date filters.
            var url = $"{instance}/api/v1/accounts/{userId}/statuses?limit=40&tagged={Uri.EscapeDataString(tag)}";
            if (maxId != null)
            {
                url += $"&max_id={maxId}";
            }

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                logger.LogError("Get tagged posts failed: {StatusCode} {Content}", response.StatusCode, errorContent);
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new HttpRequestException($"Failed to get tagged posts: {response.StatusCode}", null, response.StatusCode);
                }

                break;
            }

            var batch = await response.Content.ReadAsAsync<List<StatusMastodonResponse>>();
            if (batch == null || batch.Count == 0)
            {
                break;
            }

            foreach (var status in batch)
            {
                // Stop pagination when we reach posts older than the start date
                if (status.CreatedAt.HasValue && status.CreatedAt.Value.DateTime < since)
                {
                    hasMore = false;
                    break;
                }

                // Only include posts within the date range
                if (status.CreatedAt.HasValue && status.CreatedAt.Value.DateTime <= until)
                {
                    statuses.Add(status);
                }

                maxId = status.id;
            }

            // Simple rate limiting
            await Task.Delay(200);
        }

        return statuses;
    }
}
