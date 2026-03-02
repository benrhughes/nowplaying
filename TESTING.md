# Testing Guide

## Quick Tests

Before rebuilding the Docker image on your server, you can run a quick validation:

```bash
cd server
npm test
```

This runs a basic sanity check on all module imports and code structure without starting the actual server.

## What Gets Tested

- ✓ All dependencies can be imported
- ✓ Cheerio is using the correct export (`load` not `parse`)
- ✓ Form-data module is available
- ✓ Server code has no syntax errors
- ✓ Environment variable structure is correct

## Running Tests During Build

When you run `docker build`, the tests automatically run as part of the build process. If any test fails, the build will fail immediately before creating the image. This saves time vs. building, running, and discovering errors in logs.

## Server-Side Testing

On your server, before rebuilding:

```bash
cd ~/bcmasto/server
npm test
```

If this passes, the Docker build should succeed.

## Running the Full App

After tests pass and Docker image builds:

```bash
docker run -d \
  --name bcmasto \
  -p 3000:3000 \
  --env-file .env \
  bcmasto

# Check logs
docker logs bcmasto
```

## End-to-End Testing

Once running, test the API endpoints manually:

### Check auth status
```bash
curl http://localhost:3000/api/status
```

Should return:
```json
{"authenticated":false,"instance":"https://mastodon.social"}
```

### Test scraper
```bash
curl -X POST http://localhost:3000/api/scrape \
  -H "Content-Type: application/json" \
  -d '{"url":"https://artist.bandcamp.com/album/album-name"}'
```

Should return:
```json
{
  "title": "...",
  "artist": "...",
  "album": "...",
  "image": "...",
  "url": "..."
}
```

## Troubleshooting

If `npm test` fails with a module error:
1. Run `npm install` to update dependencies
2. Check Node.js version: `node --version` (should be 18+)
3. Delete `node_modules` and `package-lock.json`, then `npm install` again

If Docker build succeeds but container exits:
1. Check logs: `docker logs bcmasto`
2. Verify `.env` file exists and has valid values
3. Try running tests again and check for new issues
