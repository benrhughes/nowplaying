// Copyright (c) Ben Hughes. SPDX-License-Identifier: AGPL-3.0-or-later
namespace NowPlaying.Extensions;

using Microsoft.AspNetCore.Authorization;
using NowPlaying.Authorization;
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

        // In-memory registration store for app credentials per instance
        services.AddSingleton<IRegistrationStore, RegistrationStore>();

        // Register authorization handler and policy for Mastodon token validation
        services.AddScoped<IAuthorizationHandler, MastodonAuthorizationHandler>();
        services.AddAuthorization(options =>
        {
            options.AddPolicy("MastodonValid", policy => policy.RequireAuthenticatedUser().AddRequirements(new MastodonRequirement()));
        });

        // Register global exception handler
        services.AddExceptionHandler<Middleware.GlobalExceptionHandler>();
        services.AddProblemDetails();

        // Register endpoint classes for DI
        services.AddScoped<AuthenticationEndpoints>();
        services.AddScoped<PostingEndpoints>();
        services.AddScoped<HistoryEndpoints>();

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

        authGroup.MapGet("/login", (AuthenticationEndpoints e, HttpContext c, string? instance) => e.Login(c, instance));
        authGroup.MapGet("/callback", (AuthenticationEndpoints e, HttpContext c, string? code, string? state) => e.Callback(c, code, state));
        authGroup.MapGet("/logout", (AuthenticationEndpoints e, HttpContext c) => e.Logout(c));
        authGroup.MapPost("/register", (AuthenticationEndpoints e, HttpContext c, RegisterRequest r) => e.Register(c, r));
        authGroup.MapGet("/status", (AuthenticationEndpoints e, HttpContext c) => e.Status(c));

        var apiGroup = app.MapGroup("/api")
            .WithTags("API");

        var configGroup = apiGroup.MapGroup("/config")
            .WithTags("Configuration")
            .AddEndpointFilter<ValidationFilter>();

        var postingGroup = apiGroup.MapGroup("/posting")
            .WithTags("Posting")
            .RequireAuthorization("MastodonValid")
            .AddEndpointFilter<ValidationFilter>();

        postingGroup.MapPost("/scrape", (PostingEndpoints e, HttpContext c, ScrapeRequest r) => e.Scrape(c, r));
        postingGroup.MapPost("/post", (PostingEndpoints e, HttpContext c, PostRequest r) => e.Post(c, r));

        var historyGroup = apiGroup.MapGroup("/history")
            .WithTags("History")
            .RequireAuthorization("MastodonValid")
            .AddEndpointFilter<ValidationFilter>();

        historyGroup.MapGet("/search", (HistoryEndpoints e, HttpContext c, [AsParameters] HistorySearchRequest request) => e.Search(c, request));
        historyGroup.MapPost("/composite", (HistoryEndpoints e, CompositeRequest r) => e.Composite(r));
        historyGroup.MapPost("/post-composite", (HistoryEndpoints e, HttpContext c, [Microsoft.AspNetCore.Mvc.FromForm] PostCompositeRequest r) => e.PostComposite(c, r));

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
