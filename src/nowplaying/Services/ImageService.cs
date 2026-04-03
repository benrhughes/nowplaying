// Copyright (c) Ben Hughes. SPDX-License-Identifier: AGPL-3.0-or-later
namespace NowPlaying.Services;

using System.Net;
using System.Net.Sockets;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using NowPlaying.Models;

/// <summary>
/// Implementation of <see cref="IImageService"/> using a typed <see cref="HttpClient"/>.
/// </summary>
public class ImageService : IImageService
{
    private static readonly object _configLock = new();
    private static Configuration? _sharedConfig;

    private readonly HttpClient _httpClient;
    private readonly ILogger<ImageService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImageService"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="appConfig">The application configuration.</param>
    public ImageService(HttpClient httpClient, ILogger<ImageService> logger, AppConfig appConfig)
    {
        _httpClient = httpClient;
        _logger = logger;

        if (_sharedConfig == null)
        {
            lock (_configLock)
            {
                if (_sharedConfig == null)
                {
                    // Create a custom ImageSharp configuration with a capped memory pool.
                    // This prevents the Working Set from growing indefinitely by limiting how much
                    // memory the allocator can retain before returning it to the system.
                    var config = Configuration.Default.Clone();

                    if (appConfig.ImageSharpPoolLimitMb > 0)
                    {
                        // Configure the isolated memory allocator with the specified limit.
                        // ImageSharp 3.x uses the factory pattern for memory allocators.
                        config.MemoryAllocator = SixLabors.ImageSharp.Memory.MemoryAllocator.Create(new SixLabors.ImageSharp.Memory.MemoryAllocatorOptions
                        {
                            // Limits the total memory the allocator can retain in its internal pool.
                            MaximumPoolSizeMegabytes = appConfig.ImageSharpPoolLimitMb,

                            // Limits the maximum size of any single allocation to prevent OOM.
                            AllocationLimitMegabytes = appConfig.ImageSharpPoolLimitMb
                        });
                    }

                    _sharedConfig = config;
                }
            }
        }
    }

    /// <inheritdoc/>
    public async Task<byte[]> DownloadImageAsync(string url)
    {
        await ValidateUrlAsync(url);
        return await _httpClient.GetByteArrayAsync(url);
    }

    /// <inheritdoc/>
    public async Task<byte[]> GenerateCompositeAsync(IEnumerable<string> imageUrls)
    {
        var config = _sharedConfig ?? Configuration.Default;
        var urls = imageUrls.ToList();
        if (urls.Count == 0)
        {
            return Array.Empty<byte>();
        }

        const int CellSize = 300;
        int count = urls.Count;
        int cols = (int)Math.Ceiling(Math.Sqrt(count));
        int rows = (int)Math.Ceiling((double)count / cols);

        // Use the custom configuration to isolate memory pooling
        using var canvas = new Image<Rgba32>(config, cols * CellSize, rows * CellSize);
        canvas.Mutate(ctx => ctx.Fill(Color.Black));

        using var semaphore = new SemaphoreSlim(1, 1); // To synchronize drawing on the canvas
        int successCount = 0;

        try
        {
            // Limit concurrency to reduce peak memory pressure from decoding large images
            await Parallel.ForAsync(0, count, new ParallelOptions { MaxDegreeOfParallelism = 2 }, async (i, ct) =>
            {
                try
                {
                    await ValidateUrlAsync(urls[i]);
                    using var response = await _httpClient.GetAsync(urls[i], HttpCompletionOption.ResponseHeadersRead, ct);
                    response.EnsureSuccessStatusCode();

                    using var stream = await response.Content.ReadAsStreamAsync(ct);

                    // Always use the custom configuration when loading images
                    var decoderOptions = new SixLabors.ImageSharp.Formats.DecoderOptions { Configuration = config };
                    using var img = await Image.LoadAsync<Rgba32>(decoderOptions, stream, ct);

                    img.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Size = new Size(CellSize, CellSize),
                        Mode = ResizeMode.Crop,
                    }));

                    int x = (i % cols) * CellSize;
                    int y = (i / cols) * CellSize;

                    await semaphore.WaitAsync(ct);
                    try
                    {
                        canvas.Mutate(ctx => ctx.DrawImage(img, new Point(x, y), 1f));
                        Interlocked.Increment(ref successCount);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to download or load image from {url}", urls[i]);
                }
            });

            if (successCount == 0)
            {
                return Array.Empty<byte>();
            }

            using var ms = new MemoryStream();
            await canvas.SaveAsJpegAsync(ms);
            return ms.ToArray();
        }
        finally
        {
            // Explicitly release retained resources from ImageSharp's pool
            // to keep Working Set growth under control, as expected by integration tests.
            config.MemoryAllocator.ReleaseRetainedResources();
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
