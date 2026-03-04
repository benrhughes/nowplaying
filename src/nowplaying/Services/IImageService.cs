namespace NowPlaying.Services;

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

    /// <summary>
    /// Generates a composite image from a list of image URLs.
    /// </summary>
    /// <param name="imageUrls">The URLs of the images to include.</param>
    /// <returns>A byte array containing the composite image (JPEG).</returns>
    Task<byte[]> GenerateCompositeAsync(IEnumerable<string> imageUrls);
}
