namespace BcMasto.Extensions;

using System.Text.Json;

/// <summary>
/// Extensions for <see cref="HttpContent"/>.
/// </summary>
public static class HttpContentExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>
    /// Reads <see cref="HttpContent"/> as a JSON object of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="content">The content to read.</param>
    /// <returns>The deserialized object.</returns>
    public static async Task<T?> ReadAsAsync<T>(this HttpContent content)
    {
        var json = await content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }
}
