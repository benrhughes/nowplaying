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
    /// <param name="environment">The web host environment.</param>
    /// <returns>The modified service collection.</returns>
    public static IServiceCollection AddServices(this IServiceCollection services, AppConfig config, IWebHostEnvironment environment)
    {
        services.AddDistributedMemoryCache();

        services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromHours(24);
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
            options.Cookie.SecurePolicy = environment.IsProduction() ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;
        });

        // Common configuration for all typed clients
        Action<HttpClient> configureClient = client =>
        {
            client.Timeout = TimeSpan.FromSeconds(config.HttpTimeoutSeconds);
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
        // Auth routes (not under /api since they handle OAuth redirects)
        var authGroup = app.MapGroup("/auth")
            .WithTags("Authentication");

        authGroup.MapGet("/login", AuthenticationEndpoints.Login);
        authGroup.MapGet("/callback", AuthenticationEndpoints.Callback);
        authGroup.MapGet("/logout", AuthenticationEndpoints.Logout);

        var apiGroup = app.MapGroup("/api")
            .WithTags("API");

        var configGroup = apiGroup.MapGroup("/config")
            .WithTags("Configuration")
            .AddEndpointFilter<ValidationFilter>();

        configGroup.MapPost("/register", ConfigurationEndpoints.Register);
        configGroup.MapGet("/status", ConfigurationEndpoints.Status);

        var postingGroup = apiGroup.MapGroup("/posting")
            .WithTags("Posting")
            .AddEndpointFilter<ValidationFilter>();

        postingGroup.MapPost("/scrape", PostingEndpoints.Scrape);
        postingGroup.MapPost("/post", PostingEndpoints.Post);

        var historyGroup = apiGroup.MapGroup("/history")
            .WithTags("History")
            .AddEndpointFilter<ValidationFilter>();

        historyGroup.MapGet("/search", HistoryEndpoints.Search);
        historyGroup.MapPost("/composite", HistoryEndpoints.Composite);
        historyGroup.MapPost("/post-composite", HistoryEndpoints.PostComposite);

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
