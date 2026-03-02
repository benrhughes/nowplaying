class BcMasto {
  constructor() {
    this.authenticated = false;
    this.scrapedData = null;
    this.cacheElements();
    this.bindEventListeners();
    this.init();
  }

  cacheElements() {
    // Header
    this.loginBtn = document.getElementById('login-btn');
    this.logoutBtn = document.getElementById('logout-btn');
    this.loggedInText = document.getElementById('logged-in-text');

    // Content sections
    this.loginPrompt = document.getElementById('login-prompt-card');
    this.formCard = document.getElementById('form-card');
    this.previewCard = document.getElementById('preview-card');

    // Form elements
    this.urlInput = document.getElementById('url');
    this.scrapeMessage = document.getElementById('scrape-message');

    // Preview elements
    this.editArtist = document.getElementById('edit-artist');
    this.editAlbum = document.getElementById('edit-album');
    this.postText = document.getElementById('post-text');
    this.altText = document.getElementById('alt-text');
    this.previewImage = document.getElementById('preview-image');
    this.finalPreview = document.getElementById('final-preview');
    this.postBtn = document.getElementById('post-btn');
    this.postMessage = document.getElementById('post-message');
  }

  bindEventListeners() {
    // Form submission
    document.getElementById('scrape-form').addEventListener('submit', (e) => {
      e.preventDefault();
      this.scrape();
    });

    // Preview section buttons
    document.getElementById('update-preview-btn').addEventListener('click', () => {
      this.updatePreview();
    });

    this.postBtn.addEventListener('click', () => {
      this.post();
    });

    document.getElementById('back-btn').addEventListener('click', () => {
      this.scrapedData = null;
      this.updateUI();
    });

    // Auto-update preview on text changes
    this.postText.addEventListener('input', () => {
      this.updatePreviewDisplay();
    });

    this.editArtist.addEventListener('input', () => {
      this.updatePreview();
    });

    this.editAlbum.addEventListener('input', () => {
      this.updatePreview();
    });
  }

  async init() {
    await this.checkAuth();
    this.updateUI();
  }

  async checkAuth() {
    try {
      const response = await fetch('/api/status');
      const data = await response.json();
      this.authenticated = data.authenticated;
    } catch (error) {
      console.error('Auth check failed:', error);
    }
  }

  updateUI() {
    // Update header auth buttons
    if (this.authenticated) {
      this.loginBtn.style.display = 'none';
      this.logoutBtn.style.display = 'inline-block';
      this.loggedInText.style.display = 'inline-block';
    } else {
      this.loginBtn.style.display = 'inline-block';
      this.logoutBtn.style.display = 'none';
      this.loggedInText.style.display = 'none';
    }

    // Update main content sections
    if (!this.authenticated) {
      this.loginPrompt.style.display = 'block';
      this.formCard.style.display = 'none';
      this.previewCard.style.display = 'none';
    } else if (!this.scrapedData) {
      this.loginPrompt.style.display = 'none';
      this.formCard.style.display = 'block';
      this.previewCard.style.display = 'none';
      this.urlInput.value = '';
      this.scrapeMessage.textContent = '';
    } else {
      this.loginPrompt.style.display = 'none';
      this.formCard.style.display = 'none';
      this.previewCard.style.display = 'block';
      this.populatePreview();
    }
  }

  populatePreview() {
    const { artist, album, image, url } = this.scrapedData;
    const artistAlbumText = this.formatArtistAlbum(artist, album);
    const defaultText = `#nowplaying ${artistAlbumText}\n\n${url}`;

    // Populate fields
    this.editArtist.value = artist;
    this.editAlbum.value = album;
    this.postText.value = defaultText;
    this.altText.value = artistAlbumText;

    // Show/hide image
    if (image) {
      this.previewImage.src = image;
      this.previewImage.style.display = 'block';
    } else {
      this.previewImage.style.display = 'none';
    }

    // Update preview
    this.updatePreviewDisplay();
    
    // Reset button states
    this.postBtn.disabled = false;
    this.postMessage.textContent = '';
  }

  setMessage(element, text, type) {
    element.innerHTML = '';
    const div = document.createElement('div');
    div.className = `message ${type}`;
    if (type === 'info') {
      div.textContent = text;
      const spinner = document.createElement('span');
      spinner.className = 'loading';
      spinner.textContent = '⟳';
      div.appendChild(spinner);
    } else {
      div.textContent = text;
    }
    element.appendChild(div);
  }

  async scrape() {
    const url = this.urlInput.value;

    this.setMessage(this.scrapeMessage, 'Fetching album info', 'info');

    try {
      const response = await fetch('/api/scrape', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ url })
      });

      if (!response.ok) {
        const error = await response.json();
        throw new Error(error.error || 'Failed to scrape');
      }

      this.scrapedData = await response.json();
      this.updateUI();
    } catch (error) {
      this.setMessage(this.scrapeMessage, `Error: ${error.message}`, 'error');
    }
  }

  formatArtistAlbum(artist, album) {
    const cleanAlbum = album.replace(/,\s*$/, '');  // Remove trailing comma
    return `${artist} – ${cleanAlbum}`.replace(/\n/g, ' ');
  }

  updatePreview() {
    const artist = this.editArtist.value;
    const album = this.editAlbum.value;
    const artistAlbumText = this.formatArtistAlbum(artist, album);

    this.postText.value = `#nowplaying ${artistAlbumText}\n\n${this.scrapedData.url}`;
    this.altText.value = artistAlbumText;
    this.updatePreviewDisplay();
  }

  updatePreviewDisplay() {
    this.finalPreview.textContent = this.postText.value;
  }

  async post() {
    const postTextValue = this.postText.value;
    const altTextValue = this.altText.value;

    if (!postTextValue.trim()) {
      this.setMessage(this.postMessage, 'Post text cannot be empty', 'error');
      return;
    }

    this.setMessage(this.postMessage, 'Posting to Mastodon', 'info');
    this.postBtn.disabled = true;

    try {
      const response = await fetch('/api/post', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          text: postTextValue,
          altText: altTextValue,
          imageUrl: this.scrapedData.image
        })
      });

      if (!response.ok) {
        const error = await response.json();
        throw new Error(error.error || 'Failed to post');
      }

      const result = await response.json();
      this.postMessage.innerHTML = '';
      const div = document.createElement('div');
      div.className = 'message success';
      div.textContent = 'Successfully posted! ';
      const link = document.createElement('a');
      link.href = result.url;
      link.target = '_blank';
      link.textContent = 'View on Mastodon';
      div.appendChild(link);
      this.postMessage.appendChild(div);
    } catch (error) {
      this.setMessage(this.postMessage, `Error: ${error.message}`, 'error');
      this.postBtn.disabled = false;
    }
  }
}

// Initialize app when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
  new BcMasto();
});
