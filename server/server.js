import express from 'express';
import session from 'express-session';
import axios from 'axios';
import { load } from 'cheerio';
import FormData from 'form-data';
import path from 'path';
import { fileURLToPath } from 'url';
import dns from 'dns';
import https from 'https';

// Force IPv4 to avoid IPv6 timeout issues in some Docker environments
dns.setDefaultResultOrder('ipv4first');

// Custom HTTPS agent that uses IPv4 only
const httpsAgent = new https.Agent({
  family: 4  // Force IPv4
});

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const app = express();

// Environment variables
const PORT = process.env.PORT || 3000;
const REDIRECT_URI = process.env.REDIRECT_URI || 'http://localhost:3000/auth/callback';
const APP_NAME = 'BcMasto';

// Session configuration
app.use(session({
  secret: process.env.SESSION_SECRET || 'dev-secret-change-in-production',
  resave: false,
  saveUninitialized: false,
  cookie: { 
    secure: process.env.NODE_ENV === 'production',
    httpOnly: true,
    maxAge: 24 * 60 * 60 * 1000 // 24 hours
  }
}));

// Middleware
app.use(express.json());
app.use(express.static(path.join(__dirname, 'public')));

// OAuth routes
app.get('/auth/login', (req, res) => {
  const { instance, clientId } = req.session;
  
  if (!instance || !clientId) {
    return res.status(400).json({ error: 'Instance not configured. Please select an instance first.' });
  }

  const authUrl = `${instance}/oauth/authorize?client_id=${clientId}&redirect_uri=${encodeURIComponent(REDIRECT_URI)}&response_type=code&scope=write:media%20write:statuses`;
  res.redirect(authUrl);
});

app.get('/auth/callback', async (req, res) => {
  const { code } = req.query;
  const { instance, clientId, clientSecret } = req.session;
  
  if (!code) {
    return res.status(400).json({ error: 'No authorization code provided' });
  }

  if (!instance || !clientId || !clientSecret) {
    return res.status(400).json({ error: 'Session invalid. Please start the login process again.' });
  }

  try {
    // OAuth endpoints expect form-encoded data, not JSON
    const params = new URLSearchParams({
      client_id: clientId,
      client_secret: clientSecret,
      redirect_uri: REDIRECT_URI,
      grant_type: 'authorization_code',
      code
    });

    const tokenResponse = await axios.post(`${instance}/oauth/token`, params, {
      headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
      timeout: 10000,
      httpsAgent
    });

    req.session.accessToken = tokenResponse.data.access_token;
    req.session.save((err) => {
      if (err) return res.status(500).json({ error: 'Session save failed' });
      res.redirect('/');
    });
  } catch (error) {
    console.error('OAuth token exchange error:', {
      message: error.message,
      response: error.response?.data,
      status: error.response?.status,
      fullError: error
    });
    res.status(500).json({ error: 'Authentication failed' });
  }
});

app.get('/auth/logout', (req, res) => {
  req.session.destroy((err) => {
    if (err) return res.status(500).json({ error: 'Logout failed' });
    res.redirect('/');
  });
});

// API routes
app.post('/api/register', async (req, res) => {
  let { instance } = req.body;

  if (!instance) {
    return res.status(400).json({ error: 'Mastodon instance URL is required' });
  }

  // Ensure the URL is valid and properly formatted
  try {
    const url = new URL(instance);
    instance = url.origin; // Normalize to origin (no path)
  } catch {
    return res.status(400).json({ error: 'Invalid instance URL' });
  }

  try {
    // Register the app on the Mastodon instance
    const registerResponse = await axios.post(`${instance}/api/v1/apps`, {
      client_name: APP_NAME,
      redirect_uris: REDIRECT_URI,
      scopes: 'write:media write:statuses'
    }, {
      timeout: 10000,
      httpsAgent
    });

    const { client_id, client_secret } = registerResponse.data;

    // Store instance and credentials in session
    req.session.instance = instance;
    req.session.clientId = client_id;
    req.session.clientSecret = client_secret;

    req.session.save((err) => {
      if (err) return res.status(500).json({ error: 'Session save failed' });
      res.json({ success: true, instance });
    });
  } catch (error) {
    console.error('App registration error:', {
      instance,
      message: error.message,
      response: error.response?.data,
      status: error.response?.status
    });
    res.status(500).json({ error: 'Failed to register app on instance. Verify the instance URL.' });
  }
});

app.get('/api/status', (req, res) => {
  res.json({
    authenticated: !!req.session.accessToken,
    instance: req.session.instance,
    registered: !!req.session.clientId
  });
});

app.post('/api/scrape', async (req, res) => {
  const { url } = req.body;

  if (!url) {
    return res.status(400).json({ error: 'URL is required' });
  }

  // Validate URL format and restrict to Bandcamp domains
  let parsedUrl;
  try {
    parsedUrl = new URL(url);
  } catch {
    return res.status(400).json({ error: 'Invalid URL' });
  }

  if (!parsedUrl.hostname.endsWith('.bandcamp.com') && parsedUrl.hostname !== 'bandcamp.com') {
    return res.status(400).json({ error: 'Only Bandcamp URLs are supported' });
  }

  try {
    const response = await axios.get(url, { httpsAgent, timeout: 15000 });
    const $ = load(response.data);

    // Extract metadata from meta tags
    const title = $('meta[property="og:title"]').attr('content') || 
                  $('title').text();
    const image = $('meta[property="og:image"]').attr('content');
    const description = $('meta[property="og:description"]').attr('content') || '';

    // Try to extract artist and album from title or other sources
    // Bandcamp typically has format like "Artist – Album by Artist on Bandcamp"
    let artist = '';
    let album = '';

    // Try parsing from the title
    if (title) {
      const match = title.match(/(.+?)\s*–\s*(.+?)(?:\s+by|on Bandcamp)?$/i);
      if (match) {
        album = match[1].trim();
        artist = match[2].trim();
      } else {
        // Fallback: split on "by"
        const byMatch = title.match(/(.+?)\s+by\s+(.+?)(?:\s+on Bandcamp)?$/i);
        if (byMatch) {
          album = byMatch[1].trim();
          artist = byMatch[2].trim();
        }
      }
    }

    res.json({
      title,
      artist,
      album,
      image,
      description,
      url
    });
  } catch (error) {
    console.error('Scrape error:', error.message);
    res.status(500).json({ error: 'Failed to scrape URL' });
  }
});

app.post('/api/post', async (req, res) => {
  if (!req.session.accessToken || !req.session.instance) {
    return res.status(401).json({ error: 'Not authenticated' });
  }

  const { text, altText, imageUrl } = req.body;

  if (!text || !imageUrl) {
    return res.status(400).json({ error: 'Text and image are required' });
  }

  try {
    const mastodonApi = axios.create({
      baseURL: `${req.session.instance}/api/v1`,
      headers: {
        Authorization: `Bearer ${req.session.accessToken}`
      },
      httpsAgent
    });

    // Download and upload image to Mastodon
    let mediaId;
    try {
      const imageResponse = await axios.get(imageUrl, { responseType: 'arraybuffer', httpsAgent, timeout: 30000 });
      const imageBuffer = Buffer.from(imageResponse.data, 'binary');

      const form = new FormData();
      form.append('file', imageBuffer, { filename: 'album.jpg', contentType: 'image/jpeg' });
      if (altText) {
        form.append('description', altText);
      }

      const mediaResponse = await mastodonApi.post('/media', form);
      mediaId = mediaResponse.data.id;
    } catch (mediaError) {
      console.error('Media upload error:', mediaError.response?.data || mediaError.message);
      return res.status(500).json({ error: 'Failed to upload media' });
    }

    // Create status with media
    const statusResponse = await mastodonApi.post('/statuses', {
      status: text,
      media_ids: [mediaId],
      visibility: 'public'
    });

    res.json({
      success: true,
      statusId: statusResponse.data.id,
      url: statusResponse.data.url
    });
  } catch (error) {
    console.error('Post error:', error.response?.data || error.message);
    res.status(500).json({ error: 'Failed to post to Mastodon' });
  }
});

// Serve index.html for all other routes (SPA)
app.get('*', (req, res) => {
  res.sendFile(path.join(__dirname, 'public', 'index.html'));
});

app.listen(PORT, () => {
  console.log(`Server running on port ${PORT}`);
  console.log(`Redirect URI: ${REDIRECT_URI}`);
});

export default app;
