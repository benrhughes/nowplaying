namespace BcMasto.Services;

/// <summary>
/// Implementation of <see cref="IImageService"/> using a typed <see cref="HttpClient"/>.
/// </summary>
/// <param name="httpClient">The HTTP client.</param>
public class ImageService(HttpClient httpClient)
    : IImageService
{
    /// <inheritdoc/>
    public async Task<byte[]> DownloadImageAsync(string url)
    {
        return await httpClient.GetByteArrayAsync(url);
    }
}
