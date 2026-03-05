export default {
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
            <article v-else>
                <h2>Preview Post</h2>
                
                <label>
                    Artist
                    <input v-model="scrapedData.artist" @input="updatePreview">
                </label>
                
                <label>
                    Album
                    <input v-model="scrapedData.album" @input="updatePreview">
                </label>

                <label>
                    Post Text
                    <textarea v-model="postText" rows="4"></textarea>
                </label>

                <label>
                    Image Alt Text
                    <input v-model="altText">
                </label>

                <figure>
                    <img v-if="scrapedData.image" :src="scrapedData.image" :alt="altText">
                    <figcaption class="preview-text">
                        {{ postText }}
                    </figcaption>
                </figure>

                    <footer>
                        <button @click="post" :aria-busy="loading" :disabled="loading">Post to Mastodon</button>
                        <button @click="reset" class="secondary outline">Back</button>
                    </footer>
                
                <p v-if="error" class="message-error">{{ error }}</p>
                <p v-if="success" class="message-success" v-html="success"></p>
            </article>
        </div>
    `,
    props: ['authenticated', 'registered'],
    data() {
        return {
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
                const res = await fetch('/api/posting/post', {
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
