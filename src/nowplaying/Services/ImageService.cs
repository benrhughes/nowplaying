namespace NowPlaying.Services;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;

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

    /// <inheritdoc/>
    public async Task<byte[]> GenerateCompositeAsync(IEnumerable<string> imageUrls)
    {
        var urls = imageUrls.ToList();
        if (urls.Count == 0)
        {
            return Array.Empty<byte>();
        }

        var images = new List<Image>();
        try
        {
            foreach (var url in urls)
            {
                try
                {
                    var data = await this.DownloadImageAsync(url);
                    var img = Image.Load(data);
                    images.Add(img);
                }
                catch
                {
                    // Skip failed downloads or invalid images
                }
            }

            if (images.Count == 0)
            {
                return Array.Empty<byte>();
            }

            const int CellSize = 300;
            int count = images.Count;

            // Calculate grid size (aim for square-ish)
            int cols = (int)Math.Ceiling(Math.Sqrt(count));
            int rows = (int)Math.Ceiling((double)count / cols);

            using var canvas = new Image<Rgba32>(cols * CellSize, rows * CellSize);
            canvas.Mutate(ctx => ctx.Fill(Color.Black));

            for (int i = 0; i < count; i++)
            {
                var img = images[i];
                img.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(CellSize, CellSize),
                    Mode = ResizeMode.Crop,
                }));

                int x = (i % cols) * CellSize;
                int y = (i / cols) * CellSize;

                canvas.Mutate(ctx => ctx.DrawImage(img, new Point(x, y), 1f));
            }

            using var ms = new MemoryStream();
            await canvas.SaveAsJpegAsync(ms);
            return ms.ToArray();
        }
        finally
        {
            foreach (var img in images)
            {
                img.Dispose();
            }
        }
    }
}
