# BcMasto - C# Minimal APIs Implementation

A .NET 8 implementation of the Bandcamp to Mastodon poster using Minimal APIs. This is functionally equivalent to the Node.js/Express version.

## Prerequisites

- .NET 8 SDK or later
- A Mastodon instance URL
- For development: Visual Studio Code with C# extension or Visual Studio

## Getting Started

### Build

```bash
dotnet build
```

### Run

```bash
dotnet run
```

The server will start on `http://localhost:4444` by default.

### Run with custom port and redirect URI

```bash
PORT=5001 REDIRECT_URI=http://localhost:5001/auth/callback dotnet run
```

## Environment Variables

- `PORT` - Server port (default: 4444)
- `REDIRECT_URI` - OAuth redirect URI (default: http://localhost:4444/auth/callback)
- `SESSION_SECRET` - Session encryption secret (default: dev-secret-change-in-production)
- `ASPNETCORE_ENVIRONMENT` - Environment (Development/Production)

## Endpoints

All endpoints match the Node.js implementation:

### Authentication
- `GET /auth/login` - Initiate OAuth login
- `GET /auth/callback?code=...` - OAuth callback handler
- `GET /auth/logout` - Clear session

### API
- `POST /api/register` - Register app on Mastodon instance
  - Body: `{ "instance": "https://mastodon.social" }`
- `GET /api/status` - Get authentication and registration status
- `POST /api/scrape` - Scrape Bandcamp metadata
  - Body: `{ "url": "https://artist.bandcamp.com/album/title" }`
- `POST /api/post` - Post to Mastodon with image
  - Body: `{ "text": "...", "imageUrl": "...", "altText": "..." }`

## Differences from Node.js Version

### Similarities
- Same API endpoints and behaviors
- Same session management approach (in-memory for dev)
- Same OAuth flow for Mastodon
- Same HTML scraping logic
- Same validation and error handling

### Implementation Differences
- Uses Minimal APIs instead of Express
- Uses `HtmlAgilityPack` for HTML parsing instead of Cheerio
- Uses built-in `IHttpClientFactory` for HTTP requests
- Uses `IDistributedCache` for sessions (in-memory by default)
- Native JSON serialization with System.Text.Json
- No external ORM or database required (session stays in-memory)

## Development

Watch mode:
```bash
dotnet watch
```

Run tests (if added):
```bash
dotnet test
```

## Side-by-Side Comparison

Run both servers simultaneously on different ports:

### Terminal 1 - Node.js version
```bash
cd server
npm run dev
# Runs on http://localhost:3000
```

### Terminal 2 - C# version
```bash
cd csharp-server
PORT=4444 dotnet run
# Runs on http://localhost:4444
```

Then compare request/response behavior, performance, and implementation styles.

## Notes

- Session data is stored in-memory. For production, configure a distributed cache like Redis.
- The in-memory HTTP client factory is suitable for development. Consider configuring connection pooling for production.
- CORS is enabled for all origins in development. Configure appropriately for production.
