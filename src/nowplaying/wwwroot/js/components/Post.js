export default {
    template: `
        <div>
            <!-- Instance Selection (if not registered) -->
            <div v-if="!registered" class="card">
                <h2>Welcome to NowPlaying</h2>
                <p>Please enter your Mastodon instance URL to get started.</p>
                <form @submit.prevent="registerInstance" class="form-group">
                    <input v-model="instanceUrl" type="url" placeholder="https://mastodon.social" required>
                    <button type="submit" class="btn btn-primary" :disabled="loading">Connect</button>
                </form>
                <div v-if="error" class="message error">{{ error }}</div>
            </div>

            <!-- Login Prompt (if not authenticated) -->
            <div v-else-if="!authenticated" class="card">
                <h2>Connect to Mastodon</h2>
                <p>You need to authorize this app to post to your account.</p>
                <a href="/auth/login" class="btn btn-primary">Login with Mastodon</a>
            </div>

            <!-- Scrape Form -->
            <div v-else-if="!scrapedData" class="card">
                <h2>Post a Song</h2>
                <form @submit.prevent="scrape" class="form-group">
                    <label>Bandcamp URL</label>
                    <input v-model="url" type="url" placeholder="https://artist.bandcamp.com/album/..." required>
                    <button type="submit" class="btn btn-primary" :disabled="loading">
                        <span v-if="loading" class="loading" style="margin-right: 8px;"></span>
                        {{ loading ? 'Loading...' : 'Scrape' }}
                    </button>
                </form>
                <div v-if="error" class="message error">{{ error }}</div>
            </div>

            <!-- Preview & Post -->
            <div v-else class="card">
                <h2>Preview Post</h2>
                
                <div class="form-group">
                    <label>Artist</label>
                    <input v-model="scrapedData.artist" @input="updatePreview">
                </div>
                
                <div class="form-group">
                    <label>Album</label>
                    <input v-model="scrapedData.album" @input="updatePreview">
                </div>

                <div class="form-group">
                    <label>Post Text</label>
                    <textarea v-model="postText" rows="4"></textarea>
                </div>

                <div class="preview-section">
                    <img v-if="scrapedData.image" :src="scrapedData.image" class="preview-image" :alt="altText">
                    <p class="preview-text">{{ postText }}</p>
                </div>

                <div class="button-group">
                    <button @click="reset" class="btn btn-secondary">Back</button>
                    <button @click="post" class="btn btn-success" :disabled="loading">
                        <span v-if="loading" class="loading" style="margin-right: 8px;"></span>
                        {{ loading ? 'Posting...' : 'Post to Mastodon' }}
                    </button>
                </div>
                <div v-if="error" class="message error">{{ error }}</div>
                <div v-if="success" class="message success" v-html="success"></div>
            </div>
        </div>
    `,
    props: ['authenticated', 'registered'],
    data() {
        return {
            instanceUrl: '',
            url: '',
            loading: false,
            error: null,
            success: null,
            scrapedData: null,
            postText: '',
            altText: ''
        }
    },
    methods: {
        async registerInstance() {
            this.loading = true;
            this.error = null;
            try {
                const res = await fetch('/api/register', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ instance: this.instanceUrl })
                });
                if (!res.ok) throw new Error((await res.json()).error);
                // Trigger parent update or reload
                window.location.reload(); 
            } catch (e) {
                this.error = e.message;
            } finally {
                this.loading = false;
            }
        },
        async scrape() {
            this.loading = true;
            this.error = null;
            try {
                const res = await fetch('/api/scrape', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ url: this.url })
                });
                if (!res.ok) throw new Error((await res.json()).error);
                this.scrapedData = await res.json();
                this.updatePreview();
            } catch (e) {
                this.error = e.message;
            } finally {
                this.loading = false;
            }
        },
        updatePreview() {
            const { artist, album, url } = this.scrapedData;
            this.altText = `${album} by ${artist}`;
            // If text is default or empty, update it
            if (!this.postText || this.postText.includes(url)) {
                 this.postText = `#nowplaying ${artist} - ${album}\n\n${url}`;
            }
        },
        async post() {
            this.loading = true;
            this.error = null;
            this.success = null;
            try {
                const res = await fetch('/api/post', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({
                        text: this.postText,
                        imageUrl: this.scrapedData.image,
                        altText: this.altText
                    })
                });
                if (!res.ok) throw new Error((await res.json()).error);
                const data = await res.json();
                this.success = `Posted successfully! <a href="${data.url}" target="_blank">View Post</a>`;
                this.scrapedData = null;
                this.url = '';
            } catch (e) {
                this.error = e.message;
            } finally {
                this.loading = false;
            }
        },
        reset() {
            this.scrapedData = null;
            this.error = null;
            this.success = null;
        }
    }
}
