using BcMasto.Extensions;
using BcMasto.Models;

var builder = WebApplication.CreateBuilder(args);

// Configuration
var config = new AppConfig
{
    Port = int.TryParse(Environment.GetEnvironmentVariable("PORT"), out var port) ? port : 5000,
    RedirectUri = Environment.GetEnvironmentVariable("REDIRECT_URI") ?? "http://localhost:5000/auth/callback",
    SessionSecret = Environment.GetEnvironmentVariable("SESSION_SECRET") ?? "dev-secret-change-in-production"
};

// Services
builder.Services.AddSingleton(config);
builder.Services.AddBcMastoServices(config);
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

// Map endpoints
app.MapBcMastoEndpoints();

// Serve index.html for root
app.MapGet("/", context =>
{
    context.Response.ContentType = "text/html";
    return context.Response.SendFileAsync(
        Path.Combine(app.Environment.WebRootPath, "index.html"));
});

app.Run($"http://0.0.0.0:{config.Port}");
Console.WriteLine($"Server running on port {config.Port}");
Console.WriteLine($"Redirect URI: {config.RedirectUri}");
