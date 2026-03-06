# Setup Guide

## Configuration

Create a `.env` file in the root directory:

```bash
REDIRECT_URI=http://localhost:4444/auth/callback
SESSION_SECRET=change_this_to_a_random_string
PORT=4444
# ASPNETCORE_ENVIRONMENT=Production # Uncomment for production
```

## Running the Application

### Option 1: Docker Compose (Recommended)

The easiest way to run the full stack.

```bash
docker-compose up -d
```
Access at `http://localhost:4444`.

### Option 2: Local Development (.NET 10 SDK)

Required for code contributors.

```bash
cd src/nowplaying
dotnet restore
dotnet run
```

### Option 3: Production (Caddy/Nginx)

Run behind a reverse proxy handling HTTPS.

1. Set `REDIRECT_URI` to your public domain (e.g., `https://tunes.example.com/auth/callback`).
2. Configure your proxy (Caddy example):
   ```caddy
   tunes.example.com {
     reverse_proxy localhost:4444
   }
   ```
3. Run with Docker:
   ```bash
   docker run -d -p 4444:4444 --env-file .env --restart unless-stopped nowplaying
   ```

## Troubleshooting

- **"Redirect URI mismatch"**: Ensure the `REDIRECT_URI` in `.env` matches the URL you use in your browser *exactly*.
- **Login Loop**: Ensure your browser accepts cookies. If using HTTPS, ensure the certificate is valid.
