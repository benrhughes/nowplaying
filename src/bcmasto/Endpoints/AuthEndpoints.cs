using BcMasto.Models;
using BcMasto.Services;

namespace BcMasto.Endpoints;

public static class AuthEndpoints
{
    public static IResult Login(HttpContext context, string? instance, string? clientId)
    {
        instance ??= context.Session.GetString("instance");
        clientId ??= context.Session.GetString("clientId");

        if (string.IsNullOrEmpty(instance) || string.IsNullOrEmpty(clientId))
        {
            return Results.BadRequest(new ErrorResponse("Instance not configured. Please select an instance first."));
        }

        var redirectUri = context.Session.GetString("redirectUri");
        if (string.IsNullOrEmpty(redirectUri))
        {
            return Results.BadRequest(new ErrorResponse("Redirect URI not found in session. Please register your instance first."));
        }
        var authUrl = $"{instance}/oauth/authorize?" +
                      $"client_id={Uri.EscapeDataString(clientId)}" +
                      $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                      $"&response_type=code" +
                      $"&scope=write:media%20write:statuses";

        return Results.Redirect(authUrl);
    }

    public static async Task<IResult> Callback(
        HttpContext context,
        string? code,
        IMastodonService mastodonService)
    {
        if (string.IsNullOrEmpty(code))
        {
            return Results.BadRequest(new ErrorResponse("No authorization code provided"));
        }

        var instance = context.Session.GetString("instance");
        var clientId = context.Session.GetString("clientId");
        var clientSecret = context.Session.GetString("clientSecret");

        if (string.IsNullOrEmpty(instance) || string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
        {
            return Results.BadRequest(new ErrorResponse("Session invalid. Please start the login process again."));
        }

        try
        {
            var redirectUri = context.Session.GetString("redirectUri") ?? "http://localhost:4444/auth/callback";
            var accessToken = await mastodonService.GetAccessTokenAsync(instance, clientId, clientSecret, code, redirectUri);
            context.Session.SetString("accessToken", accessToken);

            return Results.Redirect("/");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"OAuth callback failed: {ex.Message}");
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    public static IResult Logout(HttpContext context)
    {
        context.Session.Clear();
        return Results.Redirect("/");
    }
}
