namespace NowPlaying.Extensions;

using NowPlaying.Endpoints;
using NowPlaying.Filters;
using NowPlaying.Models;
using NowPlaying.Services;

/// <summary>
/// Extensions for <see cref="IServiceCollection"/> and <see cref="WebApplication"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds application services to the DI container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="config">The app configuration.</param>
    /// <returns>The modified service collection.</returns>
    public static IServiceCollection AddServices(this IServiceCollection services, AppConfig config)
    {
        services.AddDistributedMemoryCache();

        services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromHours(24);
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        });

        // Common configuration for all typed clients
        Action<HttpClient> configureClient = client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("NowPlaying/1.0");
        };

        services.AddHttpClient<IImageService, ImageService>(configureClient);

        services.AddHttpClient<IMastodonService, MastodonService>(configureClient);

        services.AddHttpClient<IBandcampService, BandcampService>(configureClient);

        return services;
    }

    /// <summary>
    /// Maps the application endpoints.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>The modified web application.</returns>
    public static WebApplication MapEndpoints(this WebApplication app)
    {
        var authGroup = app.MapGroup("/auth")
            .WithTags("Authentication");

        authGroup.MapGet("/login", AuthEndpoints.Login);
        authGroup.MapGet("/callback", AuthEndpoints.Callback);
        authGroup.MapGet("/logout", AuthEndpoints.Logout);

        var apiGroup = app.MapGroup("/api")
            .WithTags("API")
            .AddEndpointFilter<ValidationFilter>();

        apiGroup.MapPost("/register", ApiEndpoints.Register);
        apiGroup.MapGet("/status", ApiEndpoints.Status);
        apiGroup.MapPost("/scrape", ApiEndpoints.Scrape);
        apiGroup.MapPost("/post", ApiEndpoints.Post);

        apiGroup.MapGet("/review/search", ReviewEndpoints.Search);
        apiGroup.MapPost("/review/composite", ReviewEndpoints.Composite);
        apiGroup.MapPost("/review/post-composite", ReviewEndpoints.PostComposite);

        // Serve index.html for root
        app.MapGet("/", context =>
        {
            context.Response.ContentType = "text/html";
            return context.Response.SendFileAsync(
                Path.Combine(app.Environment.WebRootPath, "index.html"));
        });
        return app;
    }
}
