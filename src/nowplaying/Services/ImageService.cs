// Copyright (c) Ben Hughes. SPDX-License-Identifier: AGPL-3.0-or-later
namespace NowPlaying.Services;

using System.Net;
using System.Net.Sockets;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;

/// <summary>
/// Implementation of <see cref="IImageService"/> using a typed <see cref="HttpClient"/>.
/// </summary>
/// <param name="httpClient">The HTTP client.</param>
/// <param name="logger">The logger.</param>
public class ImageService(HttpClient httpClient, ILogger<ImageService> logger)
    : IImageService
{
    /// <inheritdoc/>
    public async Task<byte[]> DownloadImageAsync(string url)
    {
        await ValidateUrlAsync(url);
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

        var images = new Image?[urls.Count];
        var tasks = new List<Task>();

        try
        {
            for (int i = 0; i < urls.Count; i++)
            {
                var index = i;
                var url = urls[i];
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        var data = await DownloadImageAsync(url);
                        images[index] = Image.Load(data);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to download or load image from {url}", url);
                    }
                }));
            }

            await Task.WhenAll(tasks);

            var validImages = images.Where(img => img != null).Cast<Image>().ToList();

            if (validImages.Count == 0)
            {
                return Array.Empty<byte>();
            }

            const int CellSize = 300;
            int count = validImages.Count;

            // Calculate grid size (aim for square-ish)
            int cols = (int)Math.Ceiling(Math.Sqrt(count));
            int rows = (int)Math.Ceiling((double)count / cols);

            using var canvas = new Image<Rgba32>(cols * CellSize, rows * CellSize);
            canvas.Mutate(ctx => ctx.Fill(Color.Black));

            for (int i = 0; i < count; i++)
            {
                var img = validImages[i];
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
                img?.Dispose();
            }
        }
    }

    private static async Task ValidateUrlAsync(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            throw new ArgumentException("Invalid URL");
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new ArgumentException("Only HTTP/HTTPS schemes are allowed");
        }

        if (uri.IsLoopback)
        {
            throw new ArgumentException("Localhost access is not allowed");
        }

        try
        {
            var ips = await Dns.GetHostAddressesAsync(uri.Host);
            foreach (var ip in ips)
            {
                if (IPAddress.IsLoopback(ip) || IsPrivateIp(ip))
                {
                    throw new ArgumentException($"Host {uri.Host} resolves to a private or loopback IP address");
                }
            }
        }
        catch (Exception ex) when (ex is not ArgumentException)
        {
            throw new ArgumentException($"Could not resolve host: {uri.Host}");
        }
    }

    private static bool IsPrivateIp(IPAddress ip)
    {
        if (ip.AddressFamily == AddressFamily.InterNetwork)
        {
            byte[] bytes = ip.GetAddressBytes();

            // 10.0.0.0/8
            if (bytes[0] == 10)
            {
                return true;
            }

            // 172.16.0.0/12
            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
            {
                return true;
            }

            // 192.168.0.0/16
            if (bytes[0] == 192 && bytes[1] == 168)
            {
                return true;
            }

            // 169.254.0.0/16 (Link-local)
            if (bytes[0] == 169 && bytes[1] == 254)
            {
                return true;
            }
        }
        else if (ip.AddressFamily == AddressFamily.InterNetworkV6)
        {
            // Unique local address (fc00::/7)
            if (ip.IsIPv6SiteLocal || ip.IsIPv6LinkLocal || (ip.GetAddressBytes()[0] & 0xFE) == 0xFC)
            {
                return true;
            }
        }

        return false;
    }
}
