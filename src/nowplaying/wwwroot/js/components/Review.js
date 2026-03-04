export default {
    template: `
        <div class="card">
            <h2>Review History</h2>
            <div class="form-group">
                <label>Date Range</label>
                <div style="display: flex; gap: 10px;">
                    <input type="date" v-model="since">
                    <input type="date" v-model="until">
                </div>
            </div>
            <button @click="search" class="btn btn-primary" :disabled="loading">
                <span v-if="loading" class="loading" style="margin-right: 8px;"></span>
                {{ loading ? 'Searching...' : 'Search #nowplaying' }}
            </button>

            <div v-if="error" class="message error">{{ error }}</div>

            <div v-if="posts.length > 0 && generating" style="margin-top: 20px; padding: 20px; background: #f0f4ff; border-radius: 8px; border: 1px solid #d0daff;">
                <div style="display: flex; align-items: center; gap: 15px;">
                    <div class="loading spinner-large"></div>
                    <div>
                        <h3 style="margin: 0;">Generating composite image...</h3>
                        <p style="margin: 5px 0 0 0; color: #666;">Processing {{ posts.length }} albums. This may take a few moments.</p>
                    </div>
                </div>
                <div class="progress-bar-container">
                    <div class="progress-bar progress-bar-animated"></div>
                </div>
            </div>
            
            <div v-if="posts.length > 0 && !generating && !compositeUrl" style="margin-top: 20px;">
                <h3>Found {{ posts.length }} albums.</h3>
            </div>
            <div v-else-if="searched && posts.length === 0" style="margin-top: 20px;">
                <p>No posts found in this range.</p>
            </div>

            <div v-if="compositeUrl" style="margin-top: 20px;">
                <h3>Composite Image</h3>
                <div style="text-align: center;">
                    <img :src="compositeUrl" style="max-width: 100%; border: 1px solid #ccc;">
                    <br>
                    <a :href="compositeUrl" download="nowplaying_composite.jpg" class="btn btn-primary" style="margin-top: 10px;">Download</a>
                    <button @click="toggleShareForm" class="btn btn-success" style="margin-left: 10px; margin-top: 10px;">Share to Mastodon</button>
                </div>
                
                <div v-if="posts.length > 0" style="margin-top: 20px; padding: 15px; background: #f9f9f9; border-radius: 4px;">
                    <h4 style="margin-top: 0;">Albums ({{ posts.length }})</h4>
                    <ol style="margin: 0; padding-left: 20px;">
                        <li v-for="(post, index) in posts" :key="post.postId" style="margin-bottom: 8px;">
                            {{ post.altText }}
                        </li>
                    </ol>
                </div>

                <!-- Share Form -->
                <div v-if="showShareForm" data-share-form style="margin-top: 20px; padding: 15px; background: #f0f0f0; border-radius: 4px; border-left: 4px solid #4CAF50;">
                    <h3>Share to Mastodon</h3>
                    
                    <div class="form-group">
                        <label>Image Alt Text</label>
                        <textarea v-model="shareAltText" rows="6" style="width: 100%; font-family: monospace; white-space: pre-wrap;"></textarea>
                    </div>

                    <div class="form-group">
                        <label>Post Text</label>
                        <textarea v-model="sharePostText" rows="5" style="width: 100%;"></textarea>
                    </div>

                    <div style="padding: 15px; background: white; border-radius: 4px; margin-top: 15px;">
                        <p style="margin: 0 0 10px 0; font-weight: bold;">Preview:</p>
                        <img :src="compositeUrl" style="max-width: 100%; border: 1px solid #ddd; margin-bottom: 10px;">
                        <p style="margin: 0 0 10px 0; color: #666; font-size: 0.9em; white-space: pre-wrap;">{{ shareAltText }}</p>
                        <p style="margin: 0; color: #333;">{{ sharePostText }}</p>
                    </div>

                    <div style="margin-top: 15px;">
                        <button @click="toggleShareForm" class="btn btn-secondary">Cancel</button>
                        <button @click="postComposite" class="btn btn-success" :disabled="shareLoading" style="margin-left: 10px;">
                            <span v-if="shareLoading" class="loading" style="margin-right: 8px;"></span>
                            {{ shareLoading ? 'Posting...' : 'Post to Mastodon' }}
                        </button>
                    </div>

                    <div v-if="shareError" class="message error" style="margin-top: 10px;">{{ shareError }}</div>
                    <div v-if="shareSuccess" class="message success" style="margin-top: 10px;" v-html="shareSuccess"></div>
                </div>
            </div>
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
            loading: false,
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
    methods: {
        async search() {
            this.loading = true;
            this.error = null;
            this.posts = [];
            this.compositeUrl = null;
            this.searched = false;
            
            try {
                const res = await fetch(`/api/review/search?since=${this.since}&until=${this.until}`);
                if (!res.ok) {
                    if (res.status === 401) {
                        this.$emit('unauthorized');
                        return;
                    }
                    throw new Error((await res.json()).error);
                }
                this.posts = await res.json();
                this.searched = true;
                
                // Automatically generate composite if posts found
                if (this.posts.length > 0) {
                    await this.generateComposite();
                }
            } catch (e) {
                this.error = e.message;
            } finally {
                this.loading = false;
            }
        },
        async generateComposite() {
            this.generating = true;
            this.error = null;
            try {
                const imageUrls = this.posts.map(p => p.imageUrl);
                const res = await fetch('/api/review/composite', {
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
                
                const res = await fetch('/api/review/post-composite', {
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
