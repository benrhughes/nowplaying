using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http.Features;
using NowPlaying.Extensions;
using NowPlaying.Models;

var builder = WebApplication.CreateBuilder(args);

// Configuration
var config = builder.Configuration.Get<AppConfig>() ?? new AppConfig();

// Validate configuration on startup
var validationResults = new List<ValidationResult>();
if (!Validator.TryValidateObject(config, new ValidationContext(config), validationResults, validateAllProperties: true))
{
    var errors = string.Join(", ", validationResults.Select(v => v.ErrorMessage));
    throw new InvalidOperationException($"Application configuration is invalid: {errors}");
}

// Services
builder.Services.AddSingleton(config);
builder.Services.AddServices(config, builder.Environment);

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 52428800; // 50MB
});

builder.Services.AddLogging();

var app = builder.Build();

// Add Content Security Policy headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("Content-Security-Policy", "default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval' https://unpkg.com; style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; img-src 'self' data: blob: https:;");
    await next();
});

app.UseSession();
app.UseStaticFiles();

app.MapEndpoints();

var logger = app.Services.GetRequiredService<ILogger<Program>>();

logger.LogInformation("Server running on port {port}", config.Port);
logger.LogInformation("Redirect URI: {redirectUri}", config.RedirectUri);

app.Run($"http://0.0.0.0:{config.Port}");
