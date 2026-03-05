export default {
    template: `
        <div>
            <article>
                <hgroup>
                    <h2>Review History</h2>
                    <p>Search your #nowplaying posts by date range. A composite image of all album covers posted in that range will be generated.</p>
                    <p>Alt-text will be naively generated from the first line of the original posts. You can edit it and the post text before sharing to Mastodon.</p>
                </hgroup>
                <div class="grid">
                    <label>
                        Start Date
                        <input type="date" v-model="since">
                    </label>
                    <label>
                        End Date
                        <input type="date" v-model="until">
                    </label>
                </div>
                <button class="btn-primary" @click="search" :aria-busy="searching" :disabled="searching || generating">
                    {{ searching ? 'Searching...' : 'Search #nowplaying' }}
                </button>
                <p v-if="error" class="message-error">{{ error }}</p>
            </article>

            <article v-if="posts.length > 0 && generating" aria-busy="true">
                Generating composite image...
                <footer>Processing {{ posts.length }} albums. This may take a few moments.</footer>
            </article>
            
            <article v-if="posts.length > 0 && !generating && !compositeUrl">
                <h3>Found {{ posts.length }} albums.</h3>
            </article>

            <article v-else-if="searched && posts.length === 0">
                <p>No posts found in this range.</p>
            </article>

            <article v-if="compositeUrl">
                <h2>Composite Image</h2>
                <figure class="text-center">
                    <img :src="compositeUrl" class="composite-image">
                </figure>

                <div>
                    <button @click="toggleShareForm">Share to Mastodon</button>
                </div>

                <details v-if="posts.length > 0">
                    <summary>View Albums ({{ posts.length }})</summary>
                    <ol>
                        <li v-for="(post, index) in posts" :key="post.postId">
                            {{ post.altText }}
                        </li>
                    </ol>
                </details>

                <!-- Share Form -->
                <article v-if="showShareForm" data-share-form>
                    <h3>Share to Mastodon</h3>
                    
                    <label>
                        Image Alt Text <span :class="['char-count', { 'over-limit': altTextCharCount > 1500 }]">{{ altTextCharCount }}/1500</span>
                        <textarea v-model="shareAltText" rows="6" maxlength="1500"></textarea>
                    </label>
                    <small v-if="altTextCharCount > 1500" class="message-error">Alt text exceeds 1500 character limit</small>

                    <label>
                        Post Text
                        <textarea v-model="sharePostText" rows="5"></textarea>
                    </label>

                    <figure>
                        <img :src="compositeUrl">
                        <figcaption>
                            <small class="preview-text">{{ shareAltText }}</small>
                            <p class="preview-text">{{ sharePostText }}</p>
                        </figcaption>
                    </figure>

                    <footer>
                        <button @click="postComposite" :aria-busy="shareLoading" :disabled="shareLoading || altTextCharCount > 1500">
                            {{ shareLoading ? 'Posting...' : 'Post to Mastodon' }}
                        </button>
                        <button @click="toggleShareForm" class="secondary outline">Cancel</button>
                    </footer>

                    <p v-if="shareError" class="message-error">{{ shareError }}</p>
                    <p v-if="shareSuccess" class="message-success" v-html="shareSuccess"></p>
                </article>
            </article>
        </div>
    `,
    data() {
        // Default to last 7 days
        const end = new Date();
        const start = new Date();
        start.setDate(end.getDate() - 7);
        
        return {
            since: start.toISOString().split('T')[0],
            until: end.toISOString().split('T')[0],
            searching: false,
            generating: false,
            error: null,
            posts: [],
            searched: false,
            compositeUrl: null,
            compositeBlob: null,
            showShareForm: false,
            shareAltText: '',
            sharePostText: '',
            shareLoading: false,
            shareError: null,
            shareSuccess: null
        }
    },
    computed: {
        altTextCharCount() {
            return this.shareAltText.length;
        }
    },
    methods: {
        async search() {
            this.searching = true;
            this.error = null;
            this.posts = [];
            this.compositeUrl = null;
            this.searched = false;
            
            try {
                const res = await fetch(`/api/history/search?since=${this.since}&until=${this.until}`);
                if (!res.ok) {
                    if (res.status === 401) {
                        this.$emit('unauthorized');
                        return;
                    }
                    throw new Error((await res.json()).error);
                }
                this.posts = await res.json();
                this.searched = true;
                this.searching = false;
                
                // Automatically generate composite if posts found
                if (this.posts.length > 0) {
                    await this.generateComposite();
                }
            } catch (e) {
                this.error = e.message;
            } finally {
                this.searching = false;
            }
        },
        async generateComposite() {
            this.generating = true;
            this.error = null;
            try {
                const imageUrls = this.posts.map(p => p.imageUrl);
                const res = await fetch('/api/history/composite', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ imageUrls })
                });
                
                if (!res.ok) throw new Error((await res.json()).error);
                
                const blob = await res.blob();
                this.compositeUrl = URL.createObjectURL(blob);
                this.compositeBlob = blob;
                
                // Initialize share form with default values
                this.initializeShareForm();
            } catch (e) {
                this.error = e.message;
            } finally {
                this.generating = false;
            }
        },
        toggleShareForm() {
            this.showShareForm = !this.showShareForm;
            this.shareError = null;
            this.shareSuccess = null;
            
            // Scroll to the share form if opening
            if (this.showShareForm) {
                this.$nextTick(() => {
                    const shareForm = document.querySelector('[data-share-form]');
                    if (shareForm) {
                        shareForm.scrollIntoView({ behavior: 'smooth', block: 'start' });
                    }
                });
            }
        },
        initializeShareForm() {
            // Create alt text as a numbered list from all albums
            const altTextLines = this.posts.map((p, i) => `${i + 1}. ${p.altText}`).join('\n');
            this.shareAltText = altTextLines;
            
            // Create default post text showing the date range
            this.sharePostText = `#nowplaying Review: ${this.since} to ${this.until}`;
        },
        async postComposite() {
            if (!this.compositeBlob) {
                this.shareError = 'No composite image available';
                return;
            }

            this.shareLoading = true;
            this.shareError = null;
            this.shareSuccess = null;
            
            try {
                const formData = new FormData();
                formData.append('image', this.compositeBlob, 'nowplaying_composite.jpg');
                formData.append('altText', this.shareAltText);
                formData.append('text', this.sharePostText);
                
                const res = await fetch('/api/history/post-composite', {
                    method: 'POST',
                    body: formData
                });
                
                if (!res.ok) {
                    const errorData = await res.json();
                    throw new Error(errorData.error || 'Failed to post composite');
                }
                
                const data = await res.json();
                this.shareSuccess = `Posted successfully! <a href="${data.url}" target="_blank">View Post</a>`;
                this.showShareForm = false;
            } catch (e) {
                this.shareError = e.message;
            } finally {
                this.shareLoading = false;
            }
        }
    }
}
