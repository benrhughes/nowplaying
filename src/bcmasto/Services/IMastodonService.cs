namespace BcMasto.Services;

public interface IMastodonService
{
    Task<(string ClientId, string ClientSecret)> RegisterAppAsync(string instance);
    Task<string> GetAccessTokenAsync(string instance, string clientId, string clientSecret, string code);
    Task<string> UploadMediaAsync(string instance, string accessToken, byte[] imageData, string? altText);
    Task<(string StatusId, string Url)> PostStatusAsync(string instance, string accessToken, string text, string mediaId);
}
