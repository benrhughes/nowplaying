# Quick Start Guide

Get the app running in 5 minutes.

## Prerequisites

- Docker installed
- A Mastodon account

## Steps

### 1. Register Mastodon App (2 minutes)

1. Go to your Mastodon instance → **Preferences** → **Development** → **New Application**
2. Name: `Bandcamp to Mastodon`
3. Redirect URI: `http://localhost:3000/auth/callback`
4. Scopes: `write:media` and `write:statuses`
5. Copy the **Client ID** and **Client Secret**

### 2. Configure (1 minute)

```bash
cp .env.example .env
# Edit .env with your Client ID and Client Secret
nano .env
```

### 3. Build (1 minute)

```bash
docker build -t bcmasto .
```

### 4. Run (1 minute)

```bash
docker run -d \
  --name bcmasto \
  -p 3000:3000 \
  --env-file .env \
  bcmasto
```

### 5. Use

1. Open `http://localhost:3000`
2. Click **Login with Mastodon**
3. Paste a Bandcamp album URL
4. Click **Fetch Album Info**
5. Review and edit if needed
6. Click **Post to Mastodon**
7. Done! 🎉

## Common Commands

### Stop the app
```bash
docker stop bcmasto
```

### View logs
```bash
docker logs bcmasto
docker logs -f bcmasto  # Follow logs
```

### Restart the app
```bash
docker restart bcmasto
```

### Remove the app
```bash
docker stop bcmasto
docker rm bcmasto
```

### Rebuild after code changes
```bash
docker build -t bcmasto .
docker stop bcmasto
docker run -d \
  --name bcmasto \
  -p 3000:3000 \
  --env-file .env \
  bcmasto
```

## Using Docker Compose (Alternative)

```bash
docker-compose up -d
docker-compose logs
docker-compose stop
docker-compose restart
```

## Production with Caddy

1. Update `.env` with production domain:
   ```
   REDIRECT_URI=https://your-domain.com/auth/callback
   NODE_ENV=production
   ```

2. Add to Caddyfile:
   ```
   bandcamp.your-domain.com {
     reverse_proxy localhost:3000
   }
   ```

3. Run: `docker run -d --name bcmasto -p 3000:3000 --env-file .env bcmasto`

## Troubleshooting

| Problem | Solution |
|---------|----------|
| Port 3000 already in use | `docker stop bcmasto` or use different port: `-p 3001:3000` |
| "Redirect URI mismatch" | Check `.env` REDIRECT_URI matches Mastodon app settings |
| Can't scrape Bandcamp | Try a different album; Bandcamp may have changed |
| Images won't upload | Check Mastodon instance allows uploads |
| Status shows in Caddy, but Mastodon doesn't load | Clear browser cache and cookies |

## For Help

- See **README.md** for detailed documentation
- See **SETUP.md** for detailed setup instructions  
- See **DEVELOPMENT.md** for code structure
- Check Docker logs: `docker logs bcmasto`

---

That's it! You're ready to automate your Bandcamp posts. 🎵
