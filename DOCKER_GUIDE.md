# BcMasto - Deployment & Architecture Guide

## Architecture Overview

### Application Stack
- **Runtime**: .NET 10.0 (C# ASP.NET Core)
- **Server**: ASP.NET Core Kestrel (reverse proxy ready)
- **Client**: Static SPA (HTML/CSS/JavaScript)
- **Database**: None (stateless API)
- **Container**: Docker + Docker Compose

### How It Works

**Yes, the C# server serves static client content.**

1. **Build Stage** (Docker)
   - Build C# application
   - Run test suite
   - Publish to `/app/publish`

2. **Runtime Stage**
   - ASP.NET Core runtime container
   - Copy client files to `wwwroot/`
   - Server runs on port 5000

3. **Request Flow**
   ```
   Client Request
        ↓
   Caddy (HTTPS) [optional]
        ↓
   ASP.NET Kestrel (:5000)
        ├─ Static files → wwwroot/ (client JS/CSS/HTML)
        ├─ /api/* → API endpoints
        └─ SPA fallback → index.html (for client-side routing)
   ```

## Docker Build Details

### Multi-stage Build Process

```dockerfile
Stage 1: Build (SDK)
  - Restore NuGet packages
  - Run tests
  - Build Release
  - Publish artifacts

Stage 2: Runtime (ASP.NET)
  - Copy published app
  - Copy client files to wwwroot/
  - Non-root user (appuser)
  - Expose port 5000
```

### Features
- ✅ Runs all tests during build (catches issues early)
- ✅ Multi-stage build (smaller final image ~400MB)
- ✅ Non-root user for security
- ✅ Health checks included
- ✅ Environment variable configuration
- ✅ .dockerignore for clean builds

## Quick Start - Local Development

```bash
# Navigate to project
cd bcmasto

# Build Docker image
# (tests are skipped by default; include --build-arg RUN_TESTS=true to run them)
docker build -t bcmasto:latest .

# Run container
docker run -p 5000:5000 \
  -e SESSION_SECRET="dev-secret-change-in-production" \
  -e REDIRECT_URI="http://localhost:5000/auth/callback" \
  bcmasto:latest

# Access at http://localhost:5000
```

## Quick Start - Docker Compose

```bash
# Local development
docker-compose up --build

# Production (with environment file)
docker-compose --env-file .env.production up -d
```

## Production Deployment

### Option 1: Cloud Provider (Recommended)

**Heroku/Railway/Render:**
```bash
heroku container:push web
heroku container:release web
```

**AWS/Azure/GCP:**
- Push image to container registry (ECR/ACR/GCR)
- Deploy to Container Service (ECS/ACI/Cloud Run)

### Option 2: VPS with Docker Compose

**Prerequisites:**
- VPS with Docker installed
- Domain name with DNS pointed to server
- SSH access

**Deployment:**
```bash
# SSH into server
ssh user@your-domain.com

# Clone repository
git clone https://github.com/yourusername/bcmasto.git
cd bcmasto

# Create production environment file
cat > .env.production << EOF
REDIRECT_URI=https://your-domain.com/auth/callback
SESSION_SECRET=$(openssl rand -base64 32)
EOF

# Start services
docker-compose -f docker-compose.yml \
  --env-file .env.production \
  up -d --build
```

### Option 3: Kubernetes

```yaml
apiVersion: v1
kind: Pod
metadata:
  name: bcmasto
spec:
  containers:
  - name: bcmasto
    image: bcmasto:latest
    ports:
    - containerPort: 5000
    env:
    - name: SESSION_SECRET
      valueFrom:
        secretKeyRef:
          name: bcmasto-secrets
          key: session-secret
    - name: REDIRECT_URI
      value: "https://your-domain.com/auth/callback"
```

## HTTPS Configuration

### Using Caddy (Recommended - Automatic HTTPS)

```bash
# 1. Uncomment Caddy in docker-compose.yml
# 2. Update Caddyfile with your domain:

example.com {
    reverse_proxy bcmasto:5000
    encode gzip
    header / X-Content-Type-Options nosniff
}

# 3. Deploy
docker-compose up -d
```

**Caddy automatically:**
- Gets HTTPS certificate from Let's Encrypt
- Redirects HTTP → HTTPS
- Renews certificates automatically

### Using Let's Encrypt + Nginx

```nginx
server {
    server_name your-domain.com;
    
    location / {
        proxy_pass http://bcmasto:5000;
        proxy_set_header X-Forwarded-Proto https;
        proxy_set_header X-Forwarded-Host $host;
    }
}
```

## Environment Configuration

### Required Variables
```bash
SESSION_SECRET=your-random-secret-32-chars-minimum
REDIRECT_URI=https://your-domain.com/auth/callback
```

### Optional Variables
```bash
PORT=5000                          # Server port
ASPNETCORE_ENVIRONMENT=Production  # Environment
```

### Generate Strong Session Secret
```bash
# Linux/macOS
openssl rand -base64 32

# Or use Python
python3 -c "import secrets; print(secrets.token_urlsafe(32))"
```

## Scaling for Production

### Single Server
- Docker Compose on VPS
- Suitable for <1000 users

### Multiple Servers
1. **Load Balancer** (e.g., HAProxy, Nginx)
   ```
   Client → Load Balancer → bcmasto-1:5000
                          → bcmasto-2:5000
                          → bcmasto-3:5000
   ```

2. **Persistent Session Storage**
   - Currently: In-memory (default)
   - For multiple instances: Add Redis
   
3. **Update appsettings.Production.json:**
   ```json
   {
     "Session": {
       "Store": "Redis",
       "ConnectionString": "redis:6379"
     }
   }
   ```

4. **Docker Compose with Redis:**
   ```yaml
   services:
     bcmasto:
       # ... existing config
       depends_on:
         - redis
     
     redis:
       image: redis:7-alpine
       ports:
         - "6379:6379"
   ```

## Monitoring & Logs

### View Logs
```bash
docker-compose logs -f bcmasto
docker-compose logs --since 1h bcmasto
```

### Health Check
```bash
curl http://localhost:5000/api/status
# Response:
# {"authenticated":false,"instance":null,"registered":false}
```

### Docker Stats
```bash
docker stats bcmasto
```

## Troubleshooting

### Container won't start
```bash
docker-compose logs bcmasto
docker-compose up --build  # rebuild
```

### Port 5000 in use
```bash
# Change in docker-compose.yml
ports:
  - "8080:5000"  # Host:Container
```

### REDIRECT_URI mismatch
- Error: "Invalid redirect_uri"
- Cause: REDIRECT_URI env var doesn't match Mastodon app registration
- Fix: Update REDIRECT_URI and re-register OAuth app on Mastodon instance

### Session not persisting
- Default: In-memory (lost on restart)
- Solution: Use Redis for persistent sessions
- Add `redis` service to docker-compose.yml

## Security Checklist

- [ ] Generate strong `SESSION_SECRET` (32+ characters)
- [ ] Use HTTPS (Caddy or reverse proxy)
- [ ] Update base images regularly
- [ ] Don't commit `.env` files with secrets
- [ ] Use environment variable substitution
- [ ] Enable CORS only for trusted origins
- [ ] Set security headers (done in Caddy config)
- [ ] Monitor container logs
- [ ] Keep dependencies updated

## Performance Tips

1. **Enable Gzip compression** (Caddy does this)
2. **Use CDN for static files** (if needed)
3. **Enable HTTP/2** (Caddy does this)
4. **Set appropriate timeouts**
5. **Monitor memory/CPU** with docker stats

## Next Steps

1. Read [DEPLOYMENT.md](./DEPLOYMENT.md) for step-by-step deployment
2. Check environment variables in `.env.example`
3. Update Caddyfile.production with your domain
4. Test locally with `docker-compose up`
5. Deploy to your infrastructure
