// Copyright (c) Ben Hughes. SPDX-License-Identifier: AGPL-3.0-or-later
using NowPlaying.Models;

namespace NowPlaying.Services;

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

    /// <summary>
    /// Verifies the user's credentials and returns their account ID.
    /// </summary>
    /// <param name="instance">The instance URL.</param>
    /// <param name="accessToken">The access token.</param>
    /// <returns>The user's account ID.</returns>
    Task<string> VerifyCredentialsAsync(string instance, string accessToken);

    /// <summary>
    /// Gets posts from a user's timeline filtered by tag and date range.
    /// </summary>
    /// <param name="instance">The instance URL.</param>
    /// <param name="accessToken">The access token.</param>
    /// <param name="userId">The user ID.</param>
    /// <param name="tag">The tag to filter by.</param>
    /// <param name="since">The start of the date range.</param>
    /// <param name="until">The end of the date range.</param>
    /// <returns>An async enumerable of matching posts.</returns>
    IAsyncEnumerable<StatusMastodonResponse> GetTaggedPostsAsync(string instance, string accessToken, string userId, string tag, DateTime since, DateTime until);
}
