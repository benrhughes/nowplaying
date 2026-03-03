# Setup Instructions for Bandcamp to Mastodon

This guide walks you through setting up and running the Bandcamp to Mastodon poster.

## Key Feature: Dynamic Instance Registration

The app supports dynamic Mastodon instance registration. You select your instance when using the app, and it automatically registers with that instance. 

## Step 1: Create Your Configuration

Create a `.env` file with your deployment settings:

1. Copy the example environment file:
   ```bash
   cp .env.example .env
   ```

2. Edit `.env` and fill in your values:
   ```
   REDIRECT_URI=http://localhost:4444/auth/callback
   SESSION_SECRET=generate-a-random-secret-here
   PORT=4444
   ```

   For `SESSION_SECRET`, you can generate a random string with:
   ```bash
   openssl rand -base64 32
   ```

3. The `REDIRECT_URI` will be used when registering with your chosen Mastodon instance in the app. Update it for production deployments.

## Step 2: Prerequisites

### For Local .NET Development
- .NET 10.0 SDK
- Install from [dotnet.microsoft.com](https://dotnet.microsoft.com/download)

### For Docker
- Docker
- Docker Compose (optional, but recommended)

## Step 3: Build the Application

### Option A: Local .NET Development

Build the project:
```bash
cd src
dotnet build
```

Run the application:
```bash
cd bcmasto
dotnet run
```

The app will start on `http://localhost:4444`

### Option B: Docker

Build the Docker image:
```bash
docker build -t bcmasto .
```

## Step 4: Run the Application

### Option A: Local .NET Development

```bash
cd src/bcmasto
dotnet run
```

Access the app at `http://localhost:4444`

### Option B: Docker (Simple)

For local testing:

```bash
docker run -d \
  --name bcmasto \
  -p 4444:4444 \
  --env-file .env \
  bcmasto
```

Access the app at `http://localhost:4444`

### Option C: Docker Compose

The project includes a `docker-compose.yml`. Run:

```bash
docker-compose up -d
```

This will start the app on port 4444.

## Step 5: First Run - Register Your Instance

1. Open the app at `http://localhost:4444`
2. You'll see the instance selection screen
3. Enter your Mastodon instance URL (e.g., `https://mastodon.social`)
4. Click "Register" - the app will automatically register itself with that instance
5. Click "Login with Mastodon"
6. Grant permissions on your Mastodon instance
7. You're now logged in and ready to use the app!

## Step 6: Using the App

1. Once logged in, paste a Bandcamp album URL in the input field
2. The app will scrape the album metadata (title, artist, cover image, description)
3. Review the preview and edit any fields if needed
4. Click "Post to Mastodon" to create a status on your instance
5. The post will include the cover art and your custom text

## Step 7: Production Setup with Caddy

Once you're ready to deploy to production:

### 1. Update Your Configuration

Update your `.env` file:
```
REDIRECT_URI=https://your-domain.com/auth/callback
SESSION_SECRET=your-random-secret
ASPNETCORE_ENVIRONMENT=Production
PORT=4444
```

### 2. Create a Caddyfile

Create or edit your Caddyfile (e.g., at `/etc/caddy/Caddyfile.d/bcmasto`):
```caddy
bandcamp.example.com {
  reverse_proxy localhost:4444
}
```

Replace `bandcamp.example.com` with your actual domain.

### 3. Reload Caddy

```bash
sudo caddy reload
```

### 4. Start the Docker Container

```bash
docker run -d \
  --name bcmasto \
  --restart unless-stopped \
  -p 4444:4444 \
  --env-file .env \
  bcmasto
```

Caddy will automatically handle HTTPS, redirects, and proxying to your .NET application.

## Stopping the App

### For Local .NET Development
Press `Ctrl+C` in the terminal running the app.

### For Docker
```bash
docker stop bcmasto
```

### For Docker Compose
```bash
docker-compose down
```

## Updating the App

### For Local Development

```bash
git pull
cd src
dotnet build
cd bcmasto
dotnet run
```

### For Docker

```bash
git pull
docker build -t bcmasto .
docker stop bcmasto
docker run -d \
  --name bcmasto \
  -p 4444:4444 \
  --env-file .env \
  bcmasto
```

### For Docker Compose

```bash
git pull
docker-compose up -d --build
```

## Troubleshooting

### Issue: "Failed to register with instance"

**Solution**: 
- Make sure you entered a valid Mastodon instance URL (e.g., `https://mastodon.social`)
- The instance must be accessible from your server
- Some instances may have registration disabled or rate limits

### Issue: "Redirect URI mismatch" after registration

**Solution**: Make sure your `REDIRECT_URI` in `.env` will be accessible from the Mastodon instance. Examples:
- Local: `http://localhost:4444/auth/callback`
- Production: `https://your-domain.com/auth/callback`

### Issue: "Failed to scrape URL"

**Solution**: 
- Check that the Bandcamp URL is valid and public
- Try a different album to test
- Bandcamp may have changed their HTML structure
- The server running the app must have internet access to fetch the Bandcamp page

### Issue: Port 4444 already in use

**Solution**: Either:
- Stop the container using port 4444: `docker ps | grep 4444` then `docker stop <container>`
- Change the port in your docker run command: `-p 5001:4444`
- Set a different PORT in your `.env` file (and update REDIRECT_URI accordingly)
- Use a different port for local development by setting `ASPNETCORE_URLS=http://localhost:5001`

### Issue: Application fails to start

**Solution**:
- Check the console output for error messages
- Make sure `.env` file is in the correct directory
- Verify REDIRECT_URI is properly formatted with `https://` or `http://`
- Check that all required environment variables are set

### Issue: "Please log in first" after registration

**Solution**:
- Session cookies may not be persisting. Check that:
  - Cookies are enabled in your browser
  - In production, you're using HTTPS (Caddy handles this)
  - The SESSION_SECRET in `.env` is set

### Issue: Images not uploading

**Solution**:
- Make sure your Mastodon instance allows image uploads
- Check that the album cover image URL is accessible from your server
- Your Mastodon instance may have file size limits (usually 10-40 MB)
- Check the application logs for specific error messages

### Issue: Can't login after selecting instance

**Solution**:
- Make sure the REDIRECT_URI in your `.env` matches what you're using to access the app
- If using Docker with a custom port, update REDIRECT_URI accordingly
- Clear browser cookies and try again
- Check application logs: `docker logs bcmasto`

## Security Reminders

- ✅ Use HTTPS in production (Caddy handles this automatically)
- ✅ Set a strong `SESSION_SECRET` using `openssl rand -base64 32`
- ✅ Never commit `.env` to git (it's in `.gitignore`)
- ✅ The app only requests `write:media` and `write:statuses` scopes (minimal permissions)
- ✅ Your access token is stored in a secure, HTTP-only cookie
- ✅ Keep your .NET dependencies updated regularly

## Environment Variables Reference

| Variable | Required | Default | Description |
|----------|----------|---------|-------------|
| `REDIRECT_URI` | Yes | `http://localhost:4444/auth/callback` | OAuth callback URL. Must match what you configured when registering the app. |
| `SESSION_SECRET` | Yes | - | Secret key for session encryption. Generate with `openssl rand -base64 32`. |
| `PORT` | No | `4444` | Port the application listens on. |
| `ASPNETCORE_ENVIRONMENT` | No | `Development` | Set to `Production` for production deployments. |

## Production Checklist

- [ ] Set strong `SESSION_SECRET` using `openssl rand -base64 32`
- [ ] Use HTTPS (Caddy or similar reverse proxy)
- [ ] Set `ASPNETCORE_ENVIRONMENT=Production`
- [ ] Update `REDIRECT_URI` to your production domain
- [ ] Never commit `.env` to version control
- [ ] Regularly update .NET dependencies: `dotnet outdated`
- [ ] Monitor application logs
- [ ] Use persistent session storage (Redis) for multi-instance setups
- [ ] Test OAuth with your actual Mastodon instance
- [ ] Test image uploads to verify media handling

## Need Help?

Check:
1. Application console output for specific error messages
2. Browser DevTools (F12) for network errors
3. Docker logs: `docker logs bcmasto`
4. [DEVELOPMENT.md](DEVELOPMENT.md) for architectural details
5. [Mastodon API documentation](https://docs.joinmastodon.org/)
6. Project issues on GitHub

Happy posting! 🎵📮
