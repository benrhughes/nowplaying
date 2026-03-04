using System.ComponentModel.DataAnnotations;
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
builder.Services.AddServices(config);
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddLogging();

var app = builder.Build();

app.UseSession();
app.UseCors();
app.UseStaticFiles();

app.MapEndpoints();

var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Server running on port {port}", config.Port);
logger.LogInformation("Redirect URI: {redirectUri}", config.RedirectUri);

app.Run($"http://0.0.0.0:{config.Port}");
