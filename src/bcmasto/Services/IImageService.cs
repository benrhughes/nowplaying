namespace BcMasto.Services;

/// <summary>
/// Service for downloading image data from external URLs.
/// </summary>
public interface IImageService
{
    /// <summary>
    /// Downloads the contents of the specified URL as a byte array.
    /// </summary>
    /// <param name="url">The URL of the image to download.</param>
    /// <returns>A byte array containing the image data.</returns>
    Task<byte[]> DownloadImageAsync(string url);
}
