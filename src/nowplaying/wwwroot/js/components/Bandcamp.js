import MastodonPost from './MastodonPost.js';

export default {
    components: { MastodonPost },
    template: `
        <div>
            <!-- Scrape Form -->
            <article v-if="!scrapedData">
                <hgroup>
                    <h2>Post a Bandcamp Album</h2>
                    <p>Enter the URL of a Bandcamp album to fetch its details. You can review and change the post before sharing to Mastodon</p>
                </hgroup>
                <form @submit.prevent="scrape">
                    <label>
                        Bandcamp URL
                        <input v-model="url" type="url" placeholder="https://artist.bandcamp.com/album/..." required>
                    </label>
                    <button type="submit" :aria-busy="loading" :disabled="loading">
                        {{ loading ? 'Getting Info...' : 'Get Album Info' }}
                    </button>
                </form>
                <p v-if="error" class="message-error">{{ error }}</p>
            </article>

            <!-- Preview & Post -->
            <MastodonPost v-else
                :initial-text="postText"
                :initial-alt-text="altText"
                :image-url="scrapedData.image"
                @posted="handlePosted"
                @cancel="reset"
                @unauthorized="$emit('unauthorized')"
            />
        </div>
    `,
    props: ['authenticated', 'registered'],
    data() {
        return {
            url: '',
            loading: false,
            error: null,
            scrapedData: null,
            postText: '',
            altText: ''
        }
    },
    methods: {
        async scrape() {
            this.loading = true;
            this.error = null;
            try {
                const res = await fetch('/api/posting/scrape', {
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
            if (!this.scrapedData) return;
            const { artist, album, url } = this.scrapedData;
            this.altText = `${album} by ${artist}`;
            this.postText = `#nowplaying ${artist} - ${album}\n\n${url}`;
        },
        handlePosted() {
            this.scrapedData = null;
            this.url = '';
        },
        reset() {
            this.scrapedData = null;
            this.error = null;
            this.url = '';
        }
    }
}
