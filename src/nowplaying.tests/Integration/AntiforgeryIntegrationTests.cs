// Copyright (c) Ben Hughes. SPDX-License-Identifier: AGPL-3.0-or-later
namespace NowPlaying.Tests.Integration;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using NowPlaying.Endpoints;
using NowPlaying.Extensions;
using NowPlaying.Models;
using Xunit;

/// <summary>
/// Integration tests verifying that the post-composite endpoint has anti-forgery disabled.
/// This ensures that [FromForm] binding doesn't cause anti-forgery validation errors
/// even though anti-forgery middleware is not configured.
/// </summary>
public class AntiforgeryIntegrationTests
{
    [Fact]
    public void PostCompositeEndpoint_HasAntiforgeryDisabled()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();

        builder.Services.AddSingleton(new AppConfig
        {
            Port = 4444,
            RedirectUri = "http://localhost:4444/auth/callback",
            SessionSecret = "test-secret"
        });

        builder.Services.AddAuthentication().AddCookie();
        builder.Services.AddAuthorization();
        builder.Services.AddScoped<HistoryEndpoints>();
        builder.Services.AddLogging();

        var app = builder.Build();

        // Act - Mapping the endpoints should not throw an InvalidOperationException about missing anti-forgery middleware.
        // Without .DisableAntiforgery() on the post-composite endpoint, this would fail with:
        // "Endpoint HTTP: POST /api/history/post-composite contains anti-forgery metadata,
        // but a middleware was not found that supports anti-forgery."
        var exception = Record.Exception(() =>
        {
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapEndpoints();
        });

        // Assert - No InvalidOperationException should occur
        Assert.Null(exception);
    }
}
