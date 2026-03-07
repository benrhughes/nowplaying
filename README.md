# NowPlaying: Bandcamp to Mastodon

A lightweight, self-hosted tool that lets you easily post Bandcamp albums to Mastodon with rich metadata and cover art.

![Screenshotof the app](image.png)

## Features

- **Post an album from Bandcamp**: Paste a Bandcamp URL, and it fetches the album art, artist, and title.
- **Search your post history**: search for posts with the #nowplaying hastag and generate a collage of the covers, then post it to Mastodon
- **Dynamic Auth**: Works with any Mastodon instance.
- **Customizable**: Edit the text before you post.
- **Privacy-First**: No user data stored on the server.

## Quick Start

1. **Configure**: Copy `.env.example` to `.env` and set your `SESSION_SECRET`.
2. **Run**:
   ```bash
   docker-compose up -d
   ```
3. **Open**: Go to `http://localhost:4444`.

See [SETUP.md](SETUP.md) for detailed installation instructions.

## Technology

- **Backend**: ASP.NET Core 10 (Minimal APIs)
- **Frontend**: Vue.js 3 (ES Modules, no build step)
- **Container**: Docker

##  Contributing

PRs are welcome. Please note this is a AGPL v3 licensed codebase and contributions will fall under than license. 

## License

Copyright (C) 2026 Ben Hughes

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Affero General Public License for more details.

You should have received a copy of the GNU Affero General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
