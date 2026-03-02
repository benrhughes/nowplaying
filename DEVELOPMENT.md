# Development Guide

This guide is for developers who want to understand or modify the codebase.

## Project Structure

```
bcmasto/
├── server/                 # Node.js/Express backend
│   ├── server.js          # Main application file
│   └── package.json       # Dependencies
├── client/                # Frontend (static files)
│   ├── index.html         # HTML template
│   ├── app.js             # JavaScript application logic
│   └── style.css          # CSS styles
├── Dockerfile             # Docker build configuration
├── docker-compose.yml     # Docker Compose configuration
├── .env.example           # Environment variable template
├── .gitignore            # Git ignore rules
├── README.md             # User guide
├── SETUP.md              # Setup instructions
└── DEVELOPMENT.md        # This file
```

## Technology Stack

- **Backend**: Node.js with Express.js
- **Frontend**: Vanilla JavaScript (no frameworks)
- **Authentication**: OAuth 2.0 (Mastodon)
- **HTML Parsing**: Cheerio
- **HTTP Client**: Axios
- **Session Management**: express-session
- **Deployment**: Docker
- **Reverse Proxy**: Caddy (recommended)

## Code Architecture

### Backend (server/server.js)

The backend is a single Express.js application with the following responsibilities:

#### OAuth Endpoints

- `GET /auth/login` - Redirects user to Mastodon OAuth authorization
- `GET /auth/callback` - Receives OAuth code and exchanges for access token
- `GET /auth/logout` - Clears session and logs user out

#### API Endpoints

- `GET /api/status` - Returns authentication status (JSON)
- `POST /api/scrape` - Scrapes Bandcamp metadata from URL
  - Input: `{ url: string }`
  - Output: `{ title, artist, album, image, description, url }`
- `POST /api/post` - Creates a Mastodon status with media
  - Input: `{ text: string, altText: string, imageUrl: string }`
  - Output: `{ success: boolean, statusId: string, url: string }`

#### Static Files

- All files in `/client` are served as static files from the root path

### Frontend (client/app.js)

The frontend is a single-page application (SPA) with a class-based architecture:

#### BcMasto Class

Manages the application state and UI:

- **Constructor**: Initializes the app
- **checkAuth()**: Fetches authentication status from server
- **render()**: Re-renders the entire UI based on state
- **renderForm()**: Shows the URL input form
- **renderPreview()**: Shows the preview with edit controls
- **scrape()**: Calls `/api/scrape` endpoint
- **updatePreview()**: Generates new preview text from edited fields
- **post()**: Calls `/api/post` endpoint

#### UI States

1. **Unauthenticated**: Shows login button
2. **Authenticated + No Data**: Shows URL form
3. **Authenticated + With Data**: Shows preview with edit controls

## Key Features Explained

### OAuth Flow

1. User clicks "Login with Mastodon"
2. Browser redirects to `GET /auth/login`
3. Server redirects to Mastodon's OAuth authorization endpoint
4. User grants permission on Mastodon
5. Browser redirects to `GET /auth/callback` with authorization code
6. Server exchanges code for access token via Mastodon API
7. Server stores token in secure session cookie
8. User is logged in

**Code Location**: `server.js` lines 37-74

### Bandcamp Metadata Extraction

The `/api/scrape` endpoint:

1. Fetches the Bandcamp page HTML
2. Uses Cheerio to parse the DOM
3. Extracts OpenGraph meta tags:
   - `og:title` → title
   - `og:image` → cover image URL
   - `og:description` → description
4. Parses artist/album from title using regex
5. Returns JSON with extracted data

**Code Location**: `server.js` lines 86-130

**Parsing Logic**: The regex tries two patterns:
- `Album – Artist` format
- `Album by Artist` format

You may need to adjust this if Bandcamp changes their metadata structure.

### Image Upload with Alt Text

The `/api/post` endpoint:

1. Downloads the album cover from the image URL
2. Uses FormData to prepare multipart upload
3. Uploads to Mastodon's `/api/v1/media` endpoint with alt text
4. Gets back a media ID
5. Creates a status with the media ID attached
6. Returns the status URL

**Code Location**: `server.js` lines 157-197

**Note**: Uses `form-data` package for multipart uploads since Axios doesn't handle FormData on Node well.

## Session Management

Sessions use `express-session` with the following flow:

1. Session data is stored in memory (default)
2. Session ID is stored in a secure HTTP-only cookie
3. Cookie is signed with `SESSION_SECRET`
4. Cookie expires after 24 hours (configurable in code)
5. In production, consider using a persistent session store (Redis, database, etc.)

**Configuration Location**: `server.js` lines 24-35

To use a persistent store, install and add:
```javascript
const RedisStore = require('connect-redis').default;
const { createClient } = require('redis');

const client = createClient();
app.use(session({
  // ... existing config ...
  store: new RedisStore({ client })
}));
```

## Development Workflow

### Local Development

1. Install Node.js (v18+)
2. Install dependencies:
   ```bash
   cd server && npm install
   ```
3. Create `.env` file with valid Mastodon credentials
4. Run with watch mode:
   ```bash
   npm run dev
   ```
5. Edit client files in `/client` - changes appear on refresh
6. Edit server files and the app will auto-reload

### Testing API Endpoints

Test the `/api/scrape` endpoint:
```bash
curl -X POST http://localhost:3000/api/scrape \
  -H "Content-Type: application/json" \
  -d '{"url":"https://example.bandcamp.com/album/test"}'
```

Test authentication status:
```bash
curl http://localhost:3000/api/status
```

### Debugging

Add console logs in `server.js` for backend debugging:
```javascript
console.error('Scrape error:', error.message);
console.log('Token response:', tokenResponse.data);
```

Browser DevTools for frontend debugging:
- F12 to open DevTools
- Network tab to inspect API calls
- Console for JavaScript errors

## Common Modifications

### Adding Form Fields

To add a new field to the preview:

1. Add input in `renderPreview()` in `app.js`
2. Read value when posting: `document.getElementById('field-name').value`
3. Include in the POST body

### Changing Mastodon Scopes

The app requests `write:media` and `write:statuses` scopes. To change:

1. Edit the scope list in `server.js` line 51:
   ```javascript
   scope=write:media%20write:statuses
   ```
2. URL-encode additional scopes with `%20` as separator
3. Re-register the app in Mastodon if scopes changed

### Parsing Different Sites

To adapt the scraper for other sites:

1. Modify the parsing logic in `/api/scrape` (line 110+)
2. Look for site-specific meta tags or HTML elements
3. Update the regex or DOM selection as needed

Example for Spotify:
```javascript
const spotifyTitle = $('h1.ScoobyFont').text();
const spotifyImage = $('img[alt="Album cover"]').attr('src');
```

### Styling Customization

Edit `client/style.css` to change:
- Colors (CSS variables recommended)
- Layout (flexbox/grid)
- Fonts
- Animations

The current design uses purple gradients; you can customize the color scheme in the CSS.

## Performance Optimization

### Caching Scraped Metadata

Currently, every scrape re-fetches the Bandcamp page. To add caching:

```javascript
const cache = new Map();

app.post('/api/scrape', async (req, res) => {
  const { url } = req.body;
  
  if (cache.has(url)) {
    return res.json(cache.get(url));
  }
  
  // ... existing scrape logic ...
  const result = { /* scraped data */ };
  cache.set(url, result);
  res.json(result);
});
```

### Session Store

For production with multiple instances, use persistent session storage:
- Redis (fast, in-memory)
- PostgreSQL (durable, scalable)
- MongoDB (flexible)

### Image Optimization

Consider compressing images before upload to reduce bandwidth.

## Error Handling

The app handles errors gracefully:

- Scrape errors → User sees "Failed to scrape URL"
- Auth errors → User sees "Authentication failed"
- Post errors → User sees error message with retry option

To improve error handling:
1. Add specific error messages in server
2. Send error codes to frontend
3. Display user-friendly messages based on error type

## Security Considerations

Current security features:
- ✅ OAuth 2.0 for authentication
- ✅ HTTP-only cookies for session storage
- ✅ Minimal Mastodon scopes (write-only)
- ✅ No sensitive data in logs

Recommended improvements:
- ☐ CSRF tokens for POST requests
- ☐ Rate limiting on API endpoints
- ☐ Content Security Policy (CSP) headers
- ☐ Input validation on all endpoints
- ☐ Request logging/monitoring
- ☐ Regular dependency updates

## Testing

Currently, there are no automated tests. To add them:

```bash
npm install --save-dev jest supertest
```

Example test:
```javascript
// test/auth.test.js
const request = require('supertest');
const app = require('../server');

describe('Auth', () => {
  test('GET /auth/login redirects to Mastodon', async () => {
    const res = await request(app).get('/auth/login');
    expect(res.status).toBe(302);
    expect(res.header.location).toContain('oauth/authorize');
  });
});
```

## Deployment Checklist

Before going live:

- [ ] Set strong `SESSION_SECRET`
- [ ] Use HTTPS (configure Caddy)
- [ ] Set `NODE_ENV=production`
- [ ] Use persistent session store
- [ ] Set up monitoring/logging
- [ ] Configure backups if needed
- [ ] Test OAuth redirect URI one more time
- [ ] Test image upload with various Mastodon instances
- [ ] Consider rate limiting
- [ ] Keep dependencies updated

## Questions or Issues?

Check:
1. Console/browser DevTools for errors
2. Docker logs: `docker logs bcmasto`
3. Environment variables in `.env`
4. Mastodon app OAuth settings
5. GitHub issues or Mastodon API docs

Happy developing! 🚀
