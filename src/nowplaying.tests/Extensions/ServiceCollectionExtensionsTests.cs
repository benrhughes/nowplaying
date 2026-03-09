// Copyright (c) Ben Hughes. SPDX-License-Identifier: AGPL-3.0-or-later
namespace NowPlaying.Tests.Extensions;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using NowPlaying.Endpoints;
using NowPlaying.Extensions;
using NowPlaying.Models;
using NowPlaying.Services;
using Xunit;

/// <summary>
/// Unit tests for the <see cref="NowPlaying.Extensions.ServiceCollectionExtensions"/> class.
/// </summary>
public class ServiceCollectionExtensionsTests
{
    /// <summary>
    /// Verifies that AddServices registers all required types in the service collection.
    /// </summary>
    [Fact]
    public void AddServices_RegistersRequiredTypes()
    {
        var services = new ServiceCollection();

        var envMock = new Mock<IWebHostEnvironment>();
        envMock.Setup(e => e.EnvironmentName).Returns("Development");
        envMock.Setup(e => e.ContentRootPath).Returns(Directory.GetCurrentDirectory());

        // minimal config required by AddServices
        var config = new AppConfig
        {
            HttpTimeoutSeconds = 5,
            Port = 0,
            RedirectUri = string.Empty,
            SessionSecret = string.Empty
        };

        services.AddLogging();
        services.AddSingleton(config);
        services.AddSingleton<IHostEnvironment>(envMock.Object);
        services.AddSingleton<IWebHostEnvironment>(envMock.Object);
        var provider = services.AddServices(config, envMock.Object).BuildServiceProvider();

        Assert.NotNull(provider.GetService<IImageService>());
        Assert.NotNull(provider.GetService<IMastodonService>());
        Assert.NotNull(provider.GetService<IBandcampService>());
        Assert.NotNull(provider.GetService<IRegistrationStore>());
        Assert.NotNull(provider.GetService<ICompositeImageCache>());
        Assert.NotNull(provider.GetService<AuthenticationEndpoints>());
        Assert.NotNull(provider.GetService<PostingEndpoints>());
        Assert.NotNull(provider.GetService<HistoryEndpoints>());
    }

    /// <summary>
    /// Verifies that AddServices sets secure cookie policy when running in production.
    /// </summary>
    [Fact]
    public void AddServices_InProduction_SetsSecureCookiePolicy()
    {
        var services = new ServiceCollection();

        var envMock = new Mock<IWebHostEnvironment>();
        envMock.Setup(e => e.EnvironmentName).Returns(Environments.Production);
        envMock.Setup(e => e.ContentRootPath).Returns(Directory.GetCurrentDirectory());

        var config = new AppConfig
        {
            HttpTimeoutSeconds = 5,
            Port = 0,
            RedirectUri = string.Empty,
            SessionSecret = string.Empty
        };

        services.AddLogging();
        services.AddSingleton(config);
        services.AddSingleton<IHostEnvironment>(envMock.Object);
        services.AddSingleton<IWebHostEnvironment>(envMock.Object);

        // This exercises the production branch of the session configuration
        var provider = services.AddServices(config, envMock.Object).BuildServiceProvider();

        // Verify session cookie policy is set to Always in production
        var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Builder.SessionOptions>>();
        Assert.Equal(Microsoft.AspNetCore.Http.CookieSecurePolicy.Always, options.Value.Cookie.SecurePolicy);
    }

    /// <summary>
    /// Verifies that MapEndpoints returns the same application instance and does not throw.
    /// </summary>
    [Fact]
    public void MapEndpoints_ReturnsSameAppAndDoesNotThrow()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddSingleton(new AppConfig
        {
            HttpTimeoutSeconds = 5,
            Port = 0,
            RedirectUri = string.Empty,
            SessionSecret = string.Empty
        });

        builder.Services.AddAuthentication().AddCookie();
        builder.Services.AddAuthorization();
        builder.Services.AddScoped<AuthenticationEndpoints>();
        builder.Services.AddScoped<PostingEndpoints>();
        builder.Services.AddScoped<HistoryEndpoints>();
        builder.Services.AddLogging();

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();

        var returned = app.MapEndpoints();
        Assert.Same(app, returned);
    }
}
