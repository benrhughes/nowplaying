# Setup Instructions for Bandcamp to Mastodon

This guide walks you through setting up and running the Bandcamp to Mastodon poster.

## Step 1: Register Your Mastodon Application

Before running the app, you need to register it with your Mastodon instance.

1. Log into your Mastodon instance (e.g., mastodon.social)
2. Go to **Preferences** → **Development** → **New Application**
3. Fill in the form:
   - **Application name**: `Bandcamp to Mastodon`
   - **Redirect URIs**: This depends on your setup:
     - **Local development**: `http://localhost:3000/auth/callback`
     - **With Caddy**: `https://your-domain.com/auth/callback` (replace with your actual domain)
   - **Scopes**: Select these two:
     - `write:media` (to upload images)
     - `write:statuses` (to create posts)
4. Click **Submit**
5. Copy the **Client ID** and **Client Secret** (you'll need these next)

## Step 2: Create Your .env File

1. Copy the example environment file:
   ```bash
   cp .env.example .env
   ```

2. Edit `.env` and fill in your values:
   ```
   MASTODON_INSTANCE=https://mastodon.social
   MASTODON_CLIENT_ID=your_client_id_here
   MASTODON_CLIENT_SECRET=your_client_secret_here
   REDIRECT_URI=http://localhost:3000/auth/callback
   SESSION_SECRET=generate-a-random-secret-here
   ```

   For `SESSION_SECRET`, you can generate a random string with:
   ```bash
   openssl rand -base64 32
   ```

3. Make sure `REDIRECT_URI` exactly matches what you registered in Mastodon!

## Step 3: Build the Docker Image

```bash
docker build -t bcmasto .
```

## Step 4a: Run with Docker (Simple)

For local testing:

```bash
docker run -d \
  --name bcmasto \
  -p 3000:3000 \
  --env-file .env \
  bcmasto
```

Access the app at `http://localhost:3000`

## Step 4b: Run with Docker Compose

Create a `docker-compose.override.yml` for local development:

```yaml
version: '3.8'
services:
  bcmasto:
    build: .
    ports:
      - "3000:3000"
    env_file: .env
    restart: unless-stopped
```

Then run:

```bash
docker-compose up -d
```

## Step 5: Production Setup with Caddy

Once you're ready to deploy:

1. Update your `.env` file:
   ```
   MASTODON_INSTANCE=https://mastodon.social
   MASTODON_CLIENT_ID=your_client_id
   MASTODON_CLIENT_SECRET=your_client_secret
   REDIRECT_URI=https://your-domain.com/auth/callback
   SESSION_SECRET=your-random-secret
   NODE_ENV=production
   ```

2. Create your Caddyfile (e.g., at `/etc/caddy/Caddyfile.d/bcmasto`):
   ```caddy
   bandcamp.example.com {
     reverse_proxy localhost:3000
   }
   ```

3. Reload Caddy:
   ```bash
   sudo caddy reload
   ```

4. Start the Docker container:
   ```bash
   docker run -d \
     --name bcmasto \
     --restart unless-stopped \
     -p 3000:3000 \
     --env-file .env \
     bcmasto
   ```

## Testing Your Setup

1. Open your app in a browser
2. Click "Login with Mastodon"
3. You should be redirected to Mastodon to authorize
4. After granting permission, you should be logged in
5. Paste a Bandcamp album URL (e.g., https://artist.bandcamp.com/album/album-name)
6. Click "Fetch Album Info"
7. Review the preview and edit if needed
8. Click "Post to Mastodon"

## Troubleshooting

### Issue: "Invalid client" or "Redirect URI mismatch"

**Solution**: Make sure your `REDIRECT_URI` in `.env` exactly matches what you registered in Mastodon's app settings, including the protocol (http/https).

### Issue: "Failed to scrape URL"

**Solution**: 
- Check that the Bandcamp URL is valid and public
- Try a different album to test
- Bandcamp may have changed their HTML structure

### Issue: Port 3000 already in use

**Solution**: Either:
- Stop the container using port 3000: `docker ps | grep 3000` then `docker stop <container>`
- Or change the port in your docker run command: `-p 3001:3000`

### Issue: Images not uploading

**Solution**:
- Make sure your Mastodon instance allows image uploads
- Check that the album cover image URL is accessible from your server
- Your Mastodon instance may have file size limits

### Issue: Sessions not persisting

**Solution**: 
- Sessions use HTTP-only cookies; make sure cookies are enabled
- In production, always use HTTPS (Caddy does this automatically)

## Stopping the App

```bash
docker stop bcmasto
```

## Updating the App

After pulling new changes:

```bash
docker build -t bcmasto .
docker stop bcmasto
docker run -d \
  --name bcmasto \
  -p 3000:3000 \
  --env-file .env \
  bcmasto
```

Or with docker-compose:

```bash
docker-compose up -d --build
```

## Security Reminders

- ✅ Use HTTPS in production (Caddy handles this)
- ✅ Set a strong `SESSION_SECRET`
- ✅ Never commit `.env` to git
- ✅ The app only requests `write:media` and `write:statuses` (minimal permissions)
- ✅ Your access token is stored in a secure, HTTP-only cookie

## Next Steps

- Customize the appearance by editing `client/style.css`
- Add more Mastodon instances by extending the session handling
- Set up monitoring/logging for your deployment
