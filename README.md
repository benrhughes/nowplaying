# NowPlaying: Bandcamp to Mastodon

A lightweight, self-hosted tool that lets you easily post Bandcamp albums to Mastodon with rich metadata and cover art.

## ✨ Features

- **Auto-Scraping**: Paste a Bandcamp URL, and it fetches the album art, artist, and title.
- **Dynamic Auth**: Works with any Mastodon instance. No hardcoded credentials.
- **Customizable**: Edit the text before you post.
- **Privacy-First**: No database required. Session-based authentication.

## 🚀 Quick Start

1. **Configure**: Copy `.env.example` to `.env` and set your `SESSION_SECRET`.
2. **Run**:
   ```bash
   docker-compose up -d
   ```
3. **Open**: Go to `http://localhost:4444`.

See [SETUP.md](SETUP.md) for detailed installation instructions.

## 🛠️ Technology

- **Backend**: ASP.NET Core 10 (Minimal APIs)
- **Frontend**: Vue.js 3 (ES Modules, no build step)
- **Container**: Docker (Alpine Linux)

## 🤝 Contributing

See [DEVELOPMENT.md](DEVELOPMENT.md) for architecture details and coding standards.

## 📄 License

GPL v3
