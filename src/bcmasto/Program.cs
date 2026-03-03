using BcMasto.Extensions;
using BcMasto.Models;

var builder = WebApplication.CreateBuilder(args);

// Configuration
var config = new AppConfig
{
    Port = int.TryParse(Environment.GetEnvironmentVariable("PORT"), out var port) ? port : 4444,
    RedirectUri = Environment.GetEnvironmentVariable("REDIRECT_URI") ?? "http://localhost:4444/auth/callback",
    SessionSecret = Environment.GetEnvironmentVariable("SESSION_SECRET") ?? "dev-secret-change-in-production"
};

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
