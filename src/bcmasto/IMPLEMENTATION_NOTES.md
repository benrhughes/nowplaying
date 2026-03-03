# C# Minimal APIs Implementation Notes

## Architecture Decisions

### 1. Minimal APIs vs Controllers
Chosen Minimal APIs for simplicity and direct endpoint definition. This mirrors the Express.js style more closely than traditional controller-based approaches.

### 2. Session Management
- Uses `IDistributedMemoryCache` for in-memory session storage
- Built-in ASP.NET Core session middleware handles serialization
- In production, swap to Redis or distributed SQL Server cache

### 3. HTTP Client Factory
- Centralized `IHttpClientFactory` for managing HttpClient instances
- Two clients configured:
  - `Default`: For general HTTP requests (15s timeout, user-agent header)
  - `Mastodon`: For Mastodon API calls (30s timeout)
- Avoids socket exhaustion and reuses connections

### 4. HTML Scraping
- Uses `HtmlAgilityPack` for robust HTML parsing
- XPath selectors match CSS selectors used in Node.js version
- Graceful fallback when meta tags unavailable

### 5. Request Models
- Separate classes for each endpoint (`RegisterRequest`, `ScrapeRequest`, `PostRequest`)
- ASP.NET Core auto-validates and deserializes
- Property names lowercase to match JSON conventions

## Key Idiomatic C# Patterns

### Minimal APIs Registration
```csharp
app.MapGet("/auth/login", (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    // Endpoint handler
});
```
- Parameters are dependency-injected automatically
- Return `IResult` for flexible responses

### Session Access
```csharp
context.Session.SetString("key", value);
var value = context.Session.GetString("key");
```
- Type-safe session access
- Built-in serialization

### HTTP Requests
```csharp
var client = httpClientFactory.CreateClient("Default");
var response = await client.PostAsJsonAsync(url, data);
var content = await response.Content.ReadAsAsync<T>();
```
- Fluent API for requests
- Automatic JSON serialization/deserialization

### Error Handling
```csharp
if (!response.IsSuccessStatusCode)
{
    return Results.StatusCode(response.StatusCode);
}
```
- Explicit null checks in C# 8+
- Proper status code propagation

## Performance Considerations

1. **Connection Pooling**: HttpClientFactory handles DNS resolution and connection reuse
2. **Async/Await**: All I/O operations are non-blocking
3. **Memory Sessions**: Suitable for single-server dev; use distributed cache in production
4. **String Regex**: Compiled patterns cached by Regex class

## Testing Comparison

### Node.js (test.js)
```javascript
const response = await fetch('http://localhost:3000/api/status');
const data = await response.json();
```

### C# (would use HttpClient or TestServer)
```csharp
var client = new HttpClient();
var response = await client.GetAsync("http://localhost:5000/api/status");
var data = await response.Content.ReadAsAsync<StatusResponse>();
```

## Future Enhancements

1. **Logging**: Wire up Serilog for structured logging
2. **Configuration**: Use Options pattern for environment-specific settings
3. **Validation**: Add FluentValidation for complex request validation
4. **Database**: Add EF Core for persistent session storage
5. **Testing**: Create xUnit test project with WebApplicationFactory
6. **Docker**: Add Dockerfile for containerized deployment
7. **OpenAPI**: Add Swagger with .AddOpenApi() (ASP.NET 8)

## Comparison with Node.js

| Aspect | Node.js | C# |
|--------|---------|-----|
| Framework | Express | ASP.NET Core Minimal APIs |
| Dependencies | 5 packages | 2 packages (built-in most functionality) |
| Session | express-session | Built-in IDistributedCache |
| HTTP Client | Axios | HttpClientFactory |
| HTML Parsing | Cheerio | HtmlAgilityPack |
| Type Safety | Dynamic/Optional | Strict by default |
| Async Model | Promise-based | async/await (similar) |
| Dev Server | node --watch | dotnet watch |
| Warm start | Instant | ~1-2 seconds |
| Memory usage | ~50MB | ~80-100MB |
