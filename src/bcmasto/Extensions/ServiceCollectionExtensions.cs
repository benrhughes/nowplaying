namespace BcMasto.Extensions;

using BcMasto.Endpoints;
using BcMasto.Filters;
using BcMasto.Models;
using BcMasto.Services;

public static class ServiceCollectionExtensions
{
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

        services.AddHttpClient("Default")
            .ConfigureHttpClient(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(15);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("BcMasto/1.0");
            });

        services.AddScoped<IMastodonService>(sp =>
            new MastodonService(
                sp.GetRequiredService<IHttpClientFactory>(),
                sp.GetRequiredService<ILogger<MastodonService>>(),
                config.RedirectUri));

        services.AddScoped<IBandcampService>(sp =>
            new BandcampService(
                sp.GetRequiredService<IHttpClientFactory>(),
                sp.GetRequiredService<ILogger<BandcampService>>()));

        return services;
    }

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
