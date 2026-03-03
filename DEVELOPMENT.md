# Development Guide

This guide is for developers who want to understand or modify the codebase.

## Project Structure

```
bcmasto/
├── src/
│   ├── bcmasto/                    # C# .NET backend application
│   │   ├── Program.cs              # Application entry point
│   │   ├── BcMasto.csproj          # Project file
│   │   ├── Endpoints/              # API endpoint definitions
│   │   │   ├── ApiEndpoints.cs     # Scrape, status, post endpoints
│   │   │   └── AuthEndpoints.cs    # OAuth flow endpoints
│   │   ├── Services/               # Business logic services
│   │   │   ├── IBandcampService.cs
│   │   │   ├── BandcampService.cs  # Bandcamp scraping
│   │   │   ├── IMastodonService.cs
│   │   │   └── MastodonService.cs  # Mastodon OAuth & posting
│   │   ├── Models/                 # Data transfer objects
│   │   │   ├── AppConfig.cs
│   │   │   ├── Requests.cs
│   │   │   └── Responses.cs
│   │   ├── Extensions/             # Extension methods
│   │   │   ├── ServiceCollectionExtensions.cs
│   │   │   └── HttpContentExtensions.cs
│   │   ├── Properties/
│   │   │   └── launchSettings.json # Development settings
│   │   ├── appsettings.json        # Configuration
│   │   └── wwwroot/                # Frontend static files (served by ASP.NET Core)
│   │       ├── index.html          # HTML template
│   │       ├── app.js              # JavaScript application logic
│   │       └── style.css           # CSS styles
│   ├── bcmasto.tests/              # Unit tests
│   │   ├── bcmasto.tests.csproj
│   │   ├── Endpoints/
│   │   ├── Services/
│   │   └── Models/
│   └── bcmasto.sln                 # Solution file
├── Dockerfile                      # Docker build configuration
├── docker-compose.yml              # Docker Compose configuration
├── .env.example                    # Environment variable template
├── .gitignore                      # Git ignore rules
├── README.md                       # User guide
├── SETUP.md                        # Setup instructions
└── DEVELOPMENT.md                  # This file
```

## Technology Stack

- **Backend**: C# with .NET (ASP.NET Core)
- **Frontend**: Vanilla JavaScript (no frameworks)
- **Authentication**: OAuth 2.0 (Mastodon)
- **HTML Parsing**: HtmlAgilityPack
- **HTTP Client**: HttpClient (built-in to .NET)
- **Session Management**: ASP.NET Core distributed session (in-memory by default)
- **Dependency Injection**: Built-in ASP.NET Core DI container
- **Deployment**: Docker
- **Reverse Proxy**: Caddy (recommended)

## Code Architecture

### Backend (C# ASP.NET Core)

The backend is an ASP.NET Core application structured with dependency injection and organized into logical services and endpoints.

#### Project Structure

- **Program.cs** - Application entry point, configures services and middleware
- **Endpoints/** - Contains endpoint definitions
  - `ApiEndpoints.cs` - Handles scraping, authentication status, and posting
  - `AuthEndpoints.cs` - Handles OAuth flow
- **Services/** - Business logic
  - `IBandcampService` / `BandcampService` - Scrapes Bandcamp metadata
  - `IMastodonService` / `MastodonService` - Handles OAuth and status posting
- **Models/** - Data transfer objects
  - `AppConfig.cs` - Configuration values
  - `Requests.cs` - Request DTOs
  - `Responses.cs` - Response DTOs
- **Extensions/** - Extension methods
  - `ServiceCollectionExtensions.cs` - DI and endpoint registration
  - `HttpContentExtensions.cs` - HTTP response parsing helpers

#### OAuth Endpoints

- `GET /auth/login` - Redirects user to Mastodon OAuth authorization
- `GET /auth/callback` - Receives OAuth code and exchanges for access token
- `GET /auth/logout` - Clears session and logs user out

#### API Endpoints

- `POST /api/register` - Registers the app with a Mastodon instance
  - Input: `{ instance: string }`
  - Output: `{ success: boolean, instance: string }`
- `GET /api/status` - Returns authentication status
  - Output: `{ authenticated: boolean, instance: string, registered: boolean }`
- `POST /api/scrape` - Scrapes Bandcamp metadata from URL
  - Input: `{ url: string }`
  - Output: `{ title, artist, album, image, description, url }`
- `POST /api/post` - Creates a Mastodon status with media
  - Input: `{ text: string, altText: string, imageUrl: string }`
  - Output: `{ success: boolean, statusId: string, url: string }`

#### Static Files

- All files in `src/bcmasto/wwwroot` are served as static files from the root path.

### Frontend (`wwwroot/app.js`)

The frontend is a single-page application (SPA) with a class-based architecture:

#### BcMasto Class

Manages the application state and UI:

- **Constructor**: Initializes the app
- **checkAuth()**: Fetches authentication status from server
- **render()**: Re-renders the entire UI based on state
- **renderForm()**: Shows the URL input form
- **renderPreview()**: Shows the preview with edit controls
- **scrape()**: Calls `/api/scrape` endpoint
- **updatePreview()**: Generates new preview text from edited fields
- **post()**: Calls `/api/post` endpoint

#### UI States

1. **Unauthenticated**: Shows login button
2. **Authenticated + No Data**: Shows URL form
3. **Authenticated + With Data**: Shows preview with edit controls

## Key Features Explained

### OAuth Flow

1. User selects a Mastodon instance and clicks "Register"
2. POST to `POST /api/register` with the instance URL
3. Server registers the app with the instance, stores credentials in session
4. User clicks "Login with Mastodon"
5. Browser redirects to `GET /auth/login`
6. Server redirects to Mastodon's OAuth authorization endpoint
7. User grants permission on Mastodon
8. Browser redirects to `GET /auth/callback` with authorization code
9. Server exchanges code for access token via Mastodon API
10. Server stores token in secure session
11. User is logged in

**Code Location**: 
- Registration: [ApiEndpoints.cs](src/bcmasto/Endpoints/ApiEndpoints.cs)
- Login/Callback: [AuthEndpoints.cs](src/bcmasto/Endpoints/AuthEndpoints.cs)

### Bandcamp Metadata Extraction

The `/api/scrape` endpoint:

1. Validates that the URL is a Bandcamp URL
2. Fetches the Bandcamp page HTML
3. Uses HtmlAgilityPack to parse the DOM
4. Extracts OpenGraph meta tags:
   - `og:title` → title
   - `og:image` → cover image URL
   - `og:description` → description
5. Parses artist/album from title using regex
6. Returns JSON with extracted data

**Code Location**: [BandcampService.cs](src/bcmasto/Services/BandcampService.cs) and [ApiEndpoints.cs](src/bcmasto/Endpoints/ApiEndpoints.cs)

**Parsing Logic**: The regex tries two patterns in [ParseArtistAndAlbum()](src/bcmasto/Services/BandcampService.cs#L45-L62):
- `Album – Artist` format
- `Album by Artist` format

You may need to adjust this if Bandcamp changes their metadata structure.

### Image Upload with Alt Text

The `/api/post` endpoint:

1. Verifies user is authenticated with access token
2. Downloads the album cover from the image URL
3. Uses MultipartFormDataContent to prepare multipart upload
4. Uploads to Mastodon's `/api/v1/media` endpoint with alt text
5. Gets back a media ID
6. Creates a status with the media ID attached
7. Returns the status URL

**Code Location**: [ApiEndpoints.cs](src/bcmasto/Endpoints/ApiEndpoints.cs) and [MastodonService.cs](src/bcmasto/Services/MastodonService.cs)

**Note**: Uses .NET's built-in MultipartFormDataContent for multipart uploads.

## Session Management

Sessions use ASP.NET Core's distributed session with the following flow:

1. Session data is stored in distributed memory cache (in-memory by default)
2. Session ID is stored in a secure HTTP-only cookie
3. Cookie is signed and encrypted automatically
4. Cookie expires after 24 hours (configurable)
5. In production, consider using a persistent session store (Redis, SQL Server, etc.)

**Configuration Location**: [ServiceCollectionExtensions.cs](src/bcmasto/Extensions/ServiceCollectionExtensions.cs)

Current session settings:
- **IdleTimeout**: 24 hours
- **HttpOnly**: true (secure against XSS)
- **SecurePolicy**: SameAsRequest (HTTPS in production)

To use a persistent store like Redis, install the Redis package and configure:
```csharp
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = Configuration.GetConnectionString("Redis");
});

services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(24);
    // ... other options
});
```

## Development Workflow

### Getting Started

Follow [SETUP.md](SETUP.md) to install prerequisites and run the application locally. Once running:

```bash
cd src/bcmasto && dotnet run
```

Application will start on `http://localhost:4444`.

### Making Changes

- **Frontend**: Edit files in `src/bcmasto/wwwroot` - changes appear on browser refresh
- **Backend**: Edit C# files - the app uses hot reload (restart to apply changes)
- **Configuration**: Edit `appsettings.Development.json` for dev settings, `.env` for environment variables

### Testing API Endpoints

Test the registration endpoint:
```bash
curl -X POST http://localhost:4444/api/register \
  -H "Content-Type: application/json" \
  -d '{"instance":"https://mastodon.social"}'
```

Test the scrape endpoint:
```bash
curl -X POST http://localhost:4444/api/scrape \
  -H "Content-Type: application/json" \
  -d '{"url":"https://example.bandcamp.com/album/test"}'
```

Test authentication status:
```bash
curl http://localhost:4444/api/status
```

### Debugging

Add debug logging in service methods:
```csharp
_logger.LogInformation("Scraping URL: {Url}", url);
_logger.LogError("Scrape failed: {Message}", ex.Message);
```

Use Visual Studio or VS Code with C# extension for:
- Breakpoints and step debugging
- Watch variables
- Call stack inspection

Browser DevTools for frontend debugging:
- F12 to open DevTools
- Network tab to inspect API calls
- Console for JavaScript errors

## Common Modifications

### Adding API Endpoints

To add a new endpoint:

1. Add a static method in `ApiEndpoints.cs` or `AuthEndpoints.cs`
2. Register it in [ServiceCollectionExtensions.cs](src/bcmasto/Extensions/ServiceCollectionExtensions.cs) using `MapGet()` or `MapPost()`
3. Example:
   ```csharp
   public static IResult MyEndpoint(HttpContext context, IMyService service)
   {
       return Results.Ok(new { message = "Hello" });
   }
   
   // In MapBcMastoEndpoints():
   apiGroup.MapGet("/my-endpoint", ApiEndpoints.MyEndpoint);
   ```

### Adding Request/Response Models

To add new request/response types:

1. Create classes in [Models/Requests.cs](src/bcmasto/Models/Requests.cs) or [Models/Responses.cs](src/bcmasto/Models/Responses.cs)
2. Use record types for immutability:
   ```csharp
   public record MyRequest(string Name, string Value);
   public record MyResponse(bool Success, string Message);
   ```
3. ASP.NET Core will automatically bind request bodies to these models

### Changing Mastodon Scopes

The app requests `write:media` and `write:statuses` scopes. To change:

1. Edit the scope list in [AuthEndpoints.cs](src/bcmasto/Endpoints/AuthEndpoints.cs):
   ```csharp
   scope=write:media%20write:statuses
   ```
2. URL-encode additional scopes with `%20` as separator
3. Update [MastodonService.cs](src/bcmasto/Services/MastodonService.cs) registration if needed

### Parsing Different Sites

To adapt the scraper for other sites:

1. Modify [BandcampService.ScrapeAsync()](src/bcmasto/Services/BandcampService.cs)
2. Use HtmlAgilityPack's XPath queries to find elements:
   ```csharp
   var titleNode = doc.DocumentNode.SelectSingleNode("//meta[@property='og:title']");
   ```
3. Update validation in [ApiEndpoints.cs](src/bcmasto/Endpoints/ApiEndpoints.cs) to accept other domains

Example for other metadata sources:
```csharp
var spotifyTitle = doc.DocumentNode.SelectSingleNode("//h1[contains(@class, 'ScoobyFont')]");
var spotifyImage = doc.DocumentNode.SelectSingleNode("//img[@alt='Album cover']");
```

### Styling Customization

Edit [wwwroot/style.css](src/bcmasto/wwwroot/style.css) to change:
- Colors (CSS variables recommended)
- Layout (flexbox/grid)
- Fonts
- Animations

The current design uses purple gradients; you can customize the color scheme in the CSS.

## Performance Optimization

### Caching Scraped Metadata

Currently, every scrape re-fetches the Bandcamp page. To add caching with IMemoryCache:

```csharp
public class BandcampService : IBandcampService
{
    private readonly IMemoryCache _cache;
    
    public async Task<ScrapeResponse> ScrapeAsync(string url)
    {
        if (_cache.TryGetValue(url, out ScrapeResponse? cached))
        {
            return cached;
        }
        
        // ... existing scrape logic ...
        _cache.Set(url, result, TimeSpan.FromHours(1));
        return result;
    }
}
```

Register the cache in [ServiceCollectionExtensions.cs](src/bcmasto/Extensions/ServiceCollectionExtensions.cs):
```csharp
services.AddMemoryCache();
```

### Session Store

For production with multiple instances, use persistent session storage:
- Redis (fast, distributed)
- SQL Server (durable, integrated)
- Distributed memory cache (scalable with NServiceBus)

Install and configure Redis:
```bash
dotnet add package StackExchange.Redis
```

### Image Optimization

Consider compressing images before upload to reduce bandwidth using ImageSharp or similar libraries.

## Error Handling

The app handles errors gracefully using try-catch blocks and HTTP status codes:

- **Scrape errors** (HTTP 500) → User sees "Failed to scrape URL"
- **Auth errors** (HTTP 500) → User sees "Authentication failed"
- **Post errors** (HTTP 500) → User sees error message with retry option
- **Bad requests** (HTTP 400) → User sees specific validation error messages
- **Unauthorized** (HTTP 401) → User sees "Please log in first"

To improve error handling:
1. Add specific error messages using [ErrorResponse](src/bcmasto/Models/Responses.cs) model
2. Send appropriate HTTP status codes (400, 401, 500)
3. Log errors using ILogger:
   ```csharp
   _logger.LogError("Scrape failed for {Url}: {Message}", url, ex.Message);
   ```
4. Display user-friendly messages in frontend based on response

## Security Considerations

Current security features:
- ✅ OAuth 2.0 for authentication
- ✅ HTTP-only cookies for session storage
- ✅ Secure session encryption (automatic in ASP.NET Core)
- ✅ Minimal Mastodon scopes (write-only)
- ✅ No sensitive data in logs
- ✅ URL validation (Bandcamp domain check)
- ✅ Input validation on request models

Recommended improvements:
- ☐ CORS policy refinement (currently allows any origin)
- ☐ Rate limiting on API endpoints
- ☐ Content Security Policy (CSP) headers
- ☐ HTTPS enforcement in production
- ☐ Request/response logging with structured logging (Serilog)
- ☐ Regular dependency updates (`dotnet outdated`)
- ☐ Add [Authorize] attributes to protected endpoints
- ☐ Implement request throttling middleware

## Testing

The project includes a test project [bcmasto.tests](src/bcmasto.tests) with unit tests using xUnit.

### Running Tests

```bash
cd src && dotnet test
```

### Test Projects

- [ApiEndpointsTests.cs](src/bcmasto.tests/Endpoints/ApiEndpointsTests.cs) - Tests API endpoints
- [AuthEndpointsTests.cs](src/bcmasto.tests/Endpoints/AuthEndpointsTests.cs) - Tests OAuth flow
- [BandcampServiceTests.cs](src/bcmasto.tests/Services/BandcampServiceTests.cs) - Tests scraping logic
- [MastodonServiceTests.cs](src/bcmasto.tests/Services/MastodonServiceTests.cs) - Tests OAuth and posting

### Adding New Tests

Example test using xUnit:
```csharp
[Fact]
public async Task Register_WithValidInstance_ReturnsSuccess()
{
    // Arrange
    var instance = "https://mastodon.social";
    var request = new RegisterRequest(instance);
    
    // Act
    var result = await ApiEndpoints.Register(context, request, service, config);
    
    // Assert
    Assert.NotNull(result);
}
```

Dependencies:
- xUnit for testing framework
- Moq for mocking services

## Deployment Checklist

Before going live:

- [ ] Set strong `SESSION_SECRET` environment variable
- [ ] Configure HTTPS (use Caddy or similar reverse proxy)
- [ ] Set `ASPNETCORE_ENVIRONMENT=Production`
- [ ] Use persistent session store (Redis recommended)
- [ ] Set up structured logging (Serilog)
- [ ] Configure CORS policy to specific domains
- [ ] Enable HTTPS redirection middleware
- [ ] Test OAuth redirect URI with production instance
- [ ] Test image upload with various Mastodon instances
- [ ] Implement rate limiting middleware
- [ ] Keep NuGet dependencies updated: `dotnet outdated` and `dotnet list package --outdated`
- [ ] Review security headers (X-Frame-Options, X-Content-Type-Options, etc.)
- [ ] Set appropriate ASPNETCORE_URLS in production
- [ ] Configure health check endpoint for load balancers
- [ ] Test graceful shutdown and restart

## History and Comparison with Node.js

This project was originally ported from a Node.js implementation. Below is a comparison of how the two stacks handle various aspects of the application.

| Aspect | Node.js | C# (Current) |
|--------|---------|-----|
| **Framework** | Express | ASP.NET Core Minimal APIs |
| **Dependencies** | 5 packages | 2 primary packages (built-in most functionality) |
| **Session** | express-session | Built-in IDistributedCache |
| **HTTP Client** | Axios | HttpClient (Typed Clients) |
| **HTML Parsing** | Cheerio | HtmlAgilityPack |
| **Type Safety** | Dynamic/Optional | Strict by default |
| **Async Model** | Promise-based | async/await (Task-based) |
| **Dev Server** | node --watch | dotnet watch |
| **Warm start** | Instant | ~1-2 seconds |
| **Memory usage** | ~50MB | ~80-100MB |

## Deployment

A `deploy.sh` script is provided in the root to quickly sync code to the production server and trigger a build.

```bash
./deploy.sh
```

It uses `rsync` to only transfer modified files and respects `.gitignore` rules, ensuring `bin/` and `obj/` folders are not transferred.

## Questions or Issues?

Check:
1. Console output for error messages
2. Browser DevTools for frontend errors (F12)
3. Application logs: run with `dotnet run --verbose`
4. Environment variables in `.env` or `appsettings.Development.json`
5. Session data in browser cookies (DevTools → Application → Cookies)
6. Mastodon instance OAuth registration settings
7. Project structure in [src/](src/) folder
8. Unit tests for examples of service usage
9. GitHub issues or Mastodon API docs

### Key Files Reference

- **Main Entry Point**: [Program.cs](src/bcmasto/Program.cs)
- **Endpoint Definitions**: [Endpoints/](src/bcmasto/Endpoints/)
- **Business Logic**: [Services/](src/bcmasto/Services/)
- **Data Models**: [Models/](src/bcmasto/Models/)
- **Configuration**: [Extensions/ServiceCollectionExtensions.cs](src/bcmasto/Extensions/ServiceCollectionExtensions.cs)
- **Frontend**: [wwwroot/](src/bcmasto/wwwroot/)

Happy developing! 🚀
