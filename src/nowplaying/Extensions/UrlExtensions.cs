// Copyright (c) Ben Hughes. SPDX-License-Identifier: AGPL-3.0-or-later
namespace NowPlaying.Extensions;

/// <summary>
/// Extensions for URL normalization and validation.
/// </summary>
public static class UrlExtensions
{
    /// <summary>
    /// Normalizes a Mastodon instance URL to ensure it has https:// and no trailing slash.
    /// </summary>
    /// <param name="instanceUrl">The instance URL to normalize.</param>
    /// <returns>The normalized instance URL.</returns>
    /// <exception cref="ArgumentException">Thrown when the URL is null, empty, or invalid.</exception>
    public static string NormalizeInstance(this string instanceUrl)
    {
        if (string.IsNullOrEmpty(instanceUrl))
        {
            throw new ArgumentException("Instance URL cannot be null or empty", nameof(instanceUrl));
        }

        // Prepare the URL for parsing
        var urlToParse = instanceUrl.Trim();
        if (!urlToParse.StartsWith("http://") && !urlToParse.StartsWith("https://"))
        {
            urlToParse = "https://" + urlToParse;
        }

        // Parse to validate and extract the authority part
        try
        {
            var uri = new Uri(urlToParse);
            var authority = uri.GetLeftPart(UriPartial.Authority).TrimEnd('/');

            // Always use https for security
            if (authority.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            {
                authority = "https://" + authority.Substring(7);
            }

            return authority;
        }
        catch (UriFormatException)
        {
            throw new ArgumentException("Invalid instance URL format", nameof(instanceUrl));
        }
    }
}
