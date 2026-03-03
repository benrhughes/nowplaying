# Bandcamp to Mastodon Poster

A simple web application that automates sharing Bandcamp albums to Mastodon. Paste a Bandcamp album link, preview the post, and publish directly to your Mastodon instance.

**Built with**: C# .NET, vanilla JavaScript, and Mastodon OAuth 2.0

## Features

- 🎵 One-click Bandcamp album metadata extraction
- 👀 Preview & edit posts before publishing  
- 🖼️ Automatic album cover upload with alt text
- 🔐 OAuth 2.0 authentication with Mastodon
- 🌐 Dynamic instance registration (register any Mastodon instance on the fly)
- 🐳 Docker containerized deployment
- 🚀 Self-hosted with minimal dependencies
- ✅ Comprehensive unit tests

## Quick Start

**See [SETUP.md](SETUP.md) for detailed installation and deployment instructions.**

Minimum steps:

1. Clone the repository
2. Create `.env` file from `.env.example`
3. Run locally with `cd src/bcmasto && dotnet run` or deploy with Docker
4. Open `http://localhost:4444`, select your Mastodon instance, and login
5. Start sharing Bandcamp albums!

## Architecture

### Backend
- **Framework**: ASP.NET Core 10.0
- **Services**: BandcampService (metadata scraping), MastodonService (OAuth & posting)
- **Session Management**: Secure HTTP-only cookies
- **API**: RESTful endpoints with dependency injection

### Frontend
- **Technology**: Vanilla JavaScript (no frameworks)
- **UI**: Single-page app with state management
- **Styling**: Responsive CSS

### Key Design Decision
The app uses **dynamic instance registration** — users select their Mastodon instance when logging in, and the app automatically registers itself. No pre-registration required.

## Documentation

- **[SETUP.md](SETUP.md)** — Installation, configuration, and deployment
- **[DEVELOPMENT.md](DEVELOPMENT.md)** — Architecture, code organization, and development guide

## Project Structure

```
bcmasto/
├── src/
│   ├── bcmasto/              # C# .NET backend
│   │   └── wwwroot/          # Frontend (HTML, CSS, JS)
│   ├── bcmasto.tests/        # Unit tests
│   └── bcmasto.sln           # Solution file
├── Dockerfile & docker-compose.yml
├── README.md, SETUP.md, DEVELOPMENT.md
└── .env.example
```

## Security

- ✅ OAuth 2.0 authentication with Mastodon
- ✅ Secure, HTTP-only session cookies
- ✅ Minimal permissions requested (`write:media`, `write:statuses`)
- ✅ HTTPS enforced in production
- ✅ No database or external dependencies required

**[See SETUP.md Production Checklist](SETUP.md#production-checklist) before deploying to production.**

## Requirements

- **To run locally**: .NET 10.0 SDK
- **To deploy**: Docker
- **To access**: Any Mastodon account on any instance

## License

MIT

## Contributing

Pull requests welcome! Please ensure tests pass and code follows C# conventions.

## Support

For setup questions, see [SETUP.md](SETUP.md).  
For development questions, see [DEVELOPMENT.md](DEVELOPMENT.md).  
For bugs, check console output and application logs.

---

Made with ❤️ for Mastodon and Bandcamp fans
