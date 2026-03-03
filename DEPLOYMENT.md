# BcMasto Deployment Guide

## Overview

BcMasto is a .NET 10.0 web application that includes:
- **Backend**: C# ASP.NET Core web server
- **Frontend**: Static client (HTML/CSS/JS) served by the backend
- **Database**: None required (stateless API)

The C# server serves static client files from the `wwwroot` directory and provides RESTful API endpoints.

## Quick Start with Docker

### Prerequisites
- Docker and Docker Compose installed
- Environment variables configured

### Building and Running

1. **Clone and navigate to the project:**
   ```bash
   cd bcmasto
   ```

2. **Create .env file (optional):**
   ```bash
   cp .env.example .env
   # Edit .env with your values if needed
   ```

3. **Build and run with Docker Compose:**
   ```bash
   docker-compose up --build
   ```

4. **Access the application:**
   - Open `http://localhost:5000` in your browser

### Environment Variables

Key environment variables:

| Variable | Default | Description |
|----------|---------|-------------|
| `PORT` | `5000` | Server port |
| `ASPNETCORE_ENVIRONMENT` | `Production` | ASP.NET environment |
| `SESSION_SECRET` | Required | Session encryption key (change in production!) |
| `REDIRECT_URI` | `http://localhost:5000/auth/callback` | OAuth redirect URI |

## Production Deployment

### Prerequisites
- Docker and Docker Compose
- Domain name with DNS configured
- Strong `SESSION_SECRET` (minimum 32 characters)

### Steps

1. **Update environment variables:**
   ```bash
   export REDIRECT_URI=https://your-domain.com/auth/callback
   export SESSION_SECRET=your-super-secret-key-min-32-chars
   ```

2. **Update docker-compose.yml:**
   - Change `http://localhost:5000` to your domain
   - Set production environment variables

3. **Use a reverse proxy (Caddy):**
   
   Uncomment the Caddy service in `docker-compose.yml` to enable HTTPS:
   
   ```yaml
   caddy:
     image: caddy:latest
     # ... (uncommented from docker-compose.yml)
   ```

4. **Update Caddyfile:**
   ```
   your-domain.com {
     reverse_proxy bcmasto:5000 {
       header_up X-Forwarded-Proto https
       header_up X-Forwarded-Host {host}
     }
   }
   ```

5. **Deploy:**
   ```bash
   docker-compose up -d --build
   ```

## Architecture

### Static Files
- Client files (HTML, CSS, JS) are copied to `wwwroot/` during Docker build
- The C# server serves static files via `app.UseStaticFiles()`
- SPA fallback ensures all routes return `index.html` for client-side routing

### API Endpoints
- `/auth/login` - OAuth login
- `/auth/callback` - OAuth callback
- `/auth/logout` - Logout
- `/api/status` - Server status
- `/api/register` - Mastodon instance registration
- `/api/scrape` - Bandcamp scraping
- `/api/post` - Post to Mastodon

### Session Management
- Uses ASP.NET Core session storage (in-memory by default)
- Configure persistence in production via `appsettings.Production.json`

## Health Checks

The Docker health check verifies:
```
curl -f http://localhost:5000/api/status
```

## Scaling Considerations

For production with multiple instances:

1. **Session State**: Migrate from in-memory to Redis
2. **Static Files**: Consider CDN for client assets
3. **Rate Limiting**: Add middleware for API protection
4. **Logging**: Configure persistent logging

## Troubleshooting

### Port already in use
```bash
# Change port in docker-compose.yml
ports:
  - "8080:5000"
```

### Environment variables not loaded
Ensure `.env` file is in the same directory as `docker-compose.yml`

### SSL/TLS issues
Use Caddy reverse proxy (uncommented in docker-compose.yml) for automatic HTTPS

### Logs
```bash
docker-compose logs -f bcmasto
```

## Security Best Practices

1. **Change `SESSION_SECRET`** - Use a strong random string (32+ characters)
2. **Use HTTPS** - Always in production (use Caddy or similar)
3. **Update base images** - Regularly rebuild Docker images
4. **Keep dependencies updated** - Monitor for security updates
5. **Validate environment** - Never commit `.env` files with secrets

## Additional Resources

- [ASP.NET Core Documentation](https://learn.microsoft.com/aspnet/core)
- [Docker Documentation](https://docs.docker.com/)
- [Caddy Documentation](https://caddyserver.com/docs/)
