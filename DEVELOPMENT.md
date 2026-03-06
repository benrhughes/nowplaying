# Development Guide

## 🏗 Architecture

The solution `nowplaying.sln` consists of:

- **src/nowplaying**: Main ASP.NET Core 10 Web App.
  - **Minimal APIs**: Endpoints defined in `Program.cs` and `Extensions/`.
  - **HTMX/Vue**: Frontend logic lives in `wwwroot/js`.
- **src/nowplaying.tests**: xUnit test project.

### Key Components

| File | Description |
|------|-------------|
| `AuthenticationEndpoints.cs` | Handles OAuth flow with Mastodon instances. |
| `PostingEndpoints.cs` | Scrapes Bandcamp and posts statuses. |
| `HistoryEndpoints.cs` | Manages local session history. |
| `AppConfig.cs` | Strongly-typed configuration wrapper. |

## 🧪 Testing

We use **xUnit** and **Moq**.

```bash
# Run all tests
dotnet test

# Run with coverage (requires coverlet)
dotnet test /p:CollectCoverage=true
```

## 📝 Coding Standards

- **Style**: Follow standard C# conventions. Treat warnings as errors.
- **HTTP Clients**: Use Typed Clients (`services.AddHttpClient<T>`).
- **DI**: Use Primary Constructors where possible.
- **Frontend**: Vue.js 3 using ES Modules (no webpack/vite build step required).

##  API Reference

### Auth
- `GET /auth/login`: Initiates OAuth.
- `GET /auth/callback`: OAuth callback handler.

### Posting
- `POST /api/posting/scrape`: Scrapes OpenGraph data from a URL.
- `POST /api/posting/post`: Uploads media and posts status to Mastodon.

### History
- `GET /api/history/search`: Returns recent posts.
