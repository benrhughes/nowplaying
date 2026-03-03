# C# Development Guide - BcMasto

This guide captures best practices and lessons learned from developing the BcMasto C# backend.

## Architecture & Design

### Dependency Injection & Configuration

**✅ DO:**
- Use dependency injection for all services
- Inject `AppConfig` via the container, not as a top-level variable
- Register services in `ServiceCollectionExtensions` for organization

```csharp
// In Program.cs
builder.Services.AddSingleton(config);
builder.Services.AddBcMastoServices(config);

// In endpoints
public static async Task<IResult> Register(
    HttpContext context,
    RegisterRequest request,
    IMastodonService mastodonService,
    AppConfig config)  // Injected parameter
{
    var redirectUri = config.RedirectUri;  // Use config parameter
}
```

**❌ DON'T:**
- Access top-level variables from `Program.cs` directly in other classes
- Create global state or singletons outside the container

### Interfaces for Testability

**✅ DO:**
- Define interfaces for all services (`IMastodonService`, `IBandcampService`)
- Mock interfaces in tests with Moq
- Pass interfaces to endpoints, not concrete implementations

```csharp
public interface IMastodonService
{
    Task<(string ClientId, string ClientSecret)> RegisterAppAsync(string instance, string redirectUri);
    Task<string> GetAccessTokenAsync(string instance, string clientId, string clientSecret, string code, string redirectUri);
}
```

**❌ DON'T:**
- Make services difficult to mock by mixing concerns
- Use extension methods that Moq can't mock (e.g., `ISession.GetString()`)

### State & Sessions

**✅ DO:**
- Use `context.Session.SetString()` / `GetString()` for storing OAuth flow state
- Store all flow-related data (instance, clientId, clientSecret, redirectUri) in session
- Retrieve from session consistently throughout the flow

```csharp
context.Session.SetString("instance", instance);
context.Session.SetString("clientId", clientId);
context.Session.SetString("clientSecret", clientSecret);
context.Session.SetString("redirectUri", redirectUri);

// Later in callback:
var instance = context.Session.GetString("instance");
var redirectUri = context.Session.GetString("redirectUri");
```

**❌ DON'T:**
- Build URLs dynamically if they need to match registered OAuth values
- Assume `context.Request.Host` and `context.Request.Scheme` will match what's registered

### OAuth & External Services

**✅ DO:**
- Use consistent, static redirect URIs registered with the external service
- Pass redirect URI as a parameter through the entire OAuth flow
- Register the redirect URI once, then reuse it everywhere

```csharp
// Register with consistent redirectUri from config
var (clientId, clientSecret) = await mastodonService.RegisterAppAsync(instance, config.RedirectUri);

// Store in session
context.Session.SetString("redirectUri", config.RedirectUri);

// Retrieve and use in callback
var redirectUri = context.Session.GetString("redirectUri");
var accessToken = await mastodonService.GetAccessTokenAsync(instance, clientId, clientSecret, code, redirectUri);
```

**❌ DON'T:**
- Build redirect URIs dynamically from the request (they may not match what the server expects)
- Register the app and then use a different redirect URI in the token exchange

## HTTP Endpoints

### Routing & HTTP Verbs

**✅ DO:**
- Use `MapGet` for endpoints accessed via browser links or GET requests
- Use `MapPost` for endpoints that process form data or JSON bodies
- Be explicit about HTTP verbs

```csharp
// Links in HTML use GET
authGroup.MapGet("/login", AuthEndpoints.Login);      // <a href="/auth/login">
authGroup.MapPost("/register", ApiEndpoints.Register); // <form method="post">
authGroup.MapGet("/callback", AuthEndpoints.Callback);  // OAuth callback is GET
```

**❌ DON'T:**
- Use `MapPost` for endpoints accessed via `<a href>` links (they'll be silently ignored)
- Mix HTTP verbs; be consistent with your API contract

### Error Handling & Fallbacks

**✅ DO:**
- Return explicit error responses (404, 400, etc.)
- Log errors with context for debugging

```csharp
if (string.IsNullOrEmpty(request.Instance))
{
    return Results.BadRequest(new ErrorResponse("Instance required"));
}

try
{
    // OAuth logic
}
catch (Exception ex)
{
    Console.WriteLine($"OAuth failed: {ex.Message}");
    return Results.StatusCode(StatusCodes.Status500InternalServerError);
}
```

**❌ DON'T:**
- Use catch-all fallback routes that return HTML (masks routing errors)
- Silently fail without logging

### Static Files & SPA Routing

**✅ DO:**
- Explicitly serve `index.html` for the root route only
- Let unmatched routes return 404 (helps catch bugs)

```csharp
app.MapGet("/", context =>
{
    context.Response.ContentType = "text/html";
    return context.Response.SendFileAsync(
        Path.Combine(app.Environment.WebRootPath, "index.html"));
});

// No fallback catch-all - other routes that don't match get 404
```

**❌ DON'T:**
- Use `MapFallback` to return `index.html` for all unmatched routes (hides API bugs)

## Testing

### Setup & Mocking

**✅ DO:**
- Mock all dependencies (services, loggers, HTTP clients)
- Create fresh mocks for each test
- Use `It.IsAny<>()` for parameters you don't care about
- Set up mocks to return realistic data structures

```csharp
public class MastodonServiceTests
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<ILogger<MastodonService>> _loggerMock;
    private readonly MastodonService _service;

    public MastodonServiceTests()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _loggerMock = new Mock<ILogger<MastodonService>>();
        _service = new MastodonService(_httpClientFactoryMock.Object, _loggerMock.Object, "http://localhost:4444/auth/callback");
    }

    [Fact]
    public async Task RegisterAppAsync_WithValidInstance_ReturnsClientIdAndSecret()
    {
        var responseData = new { client_id = "test-id", client_secret = "test-secret" };
        var httpClient = new HttpClient(new MockHttpMessageHandler(JsonSerializer.Serialize(responseData)));
        _httpClientFactoryMock.Setup(f => f.CreateClient("Default")).Returns(httpClient);

        var (clientId, clientSecret) = await _service.RegisterAppAsync("mastodon.social", "http://localhost:4444/auth/callback");

        Assert.Equal("test-id", clientId);
        Assert.Equal("test-secret", clientSecret);
    }
}
```

**❌ DON'T:**
- Test internal implementation details
- Create complex, hard-to-read test setups
- Forget to pass all required parameters (especially newly added ones)

## Configuration & Environment

### Environment Variables

**✅ DO:**
- Use environment variables for all deployment-specific config
- Provide sensible defaults for development
- Document required variables

```csharp
var config = new AppConfig
{
    Port = int.TryParse(Environment.GetEnvironmentVariable("PORT"), out var port) ? port : 4444,
    RedirectUri = Environment.GetEnvironmentVariable("REDIRECT_URI") ?? "http://localhost:4444/auth/callback",
    SessionSecret = Environment.GetEnvironmentVariable("SESSION_SECRET") ?? "dev-secret-change-in-production"
};
```

**❌ DON'T:**
- Hardcode configuration values in code
- Leave production secrets as defaults
- Assume environment variables will always be present

### AppConfig Class

**✅ DO:**
- Create a single `AppConfig` class to hold all configuration
- Register it once in the container
- Inject it where needed

```csharp
public class AppConfig
{
    public int Port { get; set; }
    public string RedirectUri { get; set; }
    public string SessionSecret { get; set; }
    public string AppName => "BcMasto";
}
```

## Docker & Containerization

### Building & Publishing

**✅ DO:**
- Use multi-stage builds (SDK for building, runtime for running)
- Run tests during the build as a quality gate
- Copy published artifacts, not source
- Use non-root users for security

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/bcmasto/BcMasto.csproj", "bcmasto/"]
RUN dotnet restore bcmasto.sln
COPY src/ .
RUN dotnet test bcmasto.tests/bcmasto.tests.csproj --no-restore
RUN cd bcmasto && dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
RUN useradd -m -u 10001 appuser && chown -R appuser:appuser /app
USER appuser
ENTRYPOINT ["dotnet", "BcMasto.dll"]
```

**❌ DON'T:**
- Run as root in containers
- Copy source code into runtime image
- Skip test execution in builds

## Common Pitfalls & Solutions

### Issue: `Cannot use local variable declared in a top-level statement`

**Cause:** Trying to access `Program.cs` top-level variables from other classes.

**Solution:** Inject via DI container instead.

```csharp
// ❌ DON'T
var config = new AppConfig { ... };
// Then try to use config in ApiEndpoints.cs

// ✅ DO
public static IResult MyEndpoint(AppConfig config) { ... }
```

### Issue: NuGet packages not found in Docker builds

**Cause:** Transitive analyzer packages failing to restore in minimal container environments.

**Solution:** Exclude analyzers in test projects, or make test steps optional.

```csharp
// In bcmasto.tests.csproj
<PackageReference Include="xunit.runner.visualstudio" Version="2.5.4">
  <ExcludeAssets>analyzers</ExcludeAssets>
  <PrivateAssets>all</PrivateAssets>
</PackageReference>
```

### Issue: Tests fail after changing method signatures

**Cause:** Forgot to update mock setups with new parameters.

**Solution:** Update all `Setup()` calls and method invocations when adding parameters.

```csharp
// After adding redirectUri parameter:
_service.RegisterAppAsync(instance, redirectUri)  // Update here
_mastodonServiceMock.Setup(m => m.RegisterAppAsync(instance, redirectUri))  // And here
```

## Code Organization

```
src/
├── bcmasto/
│   ├── Program.cs                 # Startup, DI setup
│   ├── Endpoints/
│   │   ├── ApiEndpoints.cs        # /api/* routes
│   │   └── AuthEndpoints.cs       # /auth/* routes
│   ├── Services/
│   │   ├── IMastodonService.cs
│   │   ├── MastodonService.cs
│   │   ├── IBandcampService.cs
│   │   └── BandcampService.cs
│   ├── Models/
│   │   ├── AppConfig.cs
│   │   ├── Requests.cs
│   │   └── Responses.cs
│   └── Extensions/
│       └── ServiceCollectionExtensions.cs
└── bcmasto.tests/
    ├── Services/
    ├── Endpoints/
    └── Models/
```

Keep tests mirroring the structure of the main project for easy navigation.

## Summary

- **Dependency Injection** - Everything goes through the DI container
- **Consistency** - Static config values, not dynamic URLs for external services
- **Explicit Routing** - Clear HTTP verbs, no magic fallbacks
- **Testability** - Interfaces, mocks, sensible defaults
- **Error Handling** - Log and return explicit status codes
- **Configuration** - Environment variables with sensible dev defaults
