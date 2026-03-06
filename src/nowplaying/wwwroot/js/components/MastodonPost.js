export default {
    template: `
        <article>
            <h2>Preview & Post</h2>
            
            <label>
                Post Text
                <textarea v-model="text" rows="4"></textarea>
            </label>

            <label>
                Image Alt Text <span :class="['char-count', { 'over-limit': altTextCharCount > 1500 }]">{{ altTextCharCount }}/1500</span>
                <textarea v-model="altText" rows="2" maxlength="1500"></textarea>
            </label>
            <small v-if="altTextCharCount > 1500" class="message-error">Alt text exceeds 1500 character limit</small>

            <figure>
                <img :src="previewUrl" :alt="altText" class="preview-image">
                <figcaption class="preview-text">
                    {{ text }}
                </figcaption>
            </figure>

            <footer>
                <button @click="post" :aria-busy="loading" :disabled="loading || altTextCharCount > 1500">Post to Mastodon</button>
                <button @click="$emit('cancel')" class="secondary outline">Back</button>
            </footer>
            
            <p v-if="error" class="message-error">{{ error }}</p>
            <p v-if="success" class="message-success" v-html="success"></p>
        </article>
    `,
    props: {
        initialText: String,
        initialAltText: String,
        imageUrl: String,
        imageBlob: Blob
    },
    data() {
        return {
            text: this.initialText || '',
            altText: this.initialAltText || '',
            loading: false,
            error: null,
            success: null,
            blobPreviewUrl: null
        }
    },
    computed: {
        altTextCharCount() {
            return this.altText ? this.altText.length : 0;
        },
        previewUrl() {
            if (this.imageBlob) {
                if (!this.blobPreviewUrl) {
                    this.blobPreviewUrl = URL.createObjectURL(this.imageBlob);
                }
                return this.blobPreviewUrl;
            }
            return this.imageUrl;
        }
    },
    beforeUnmount() {
        if (this.blobPreviewUrl) {
            URL.revokeObjectURL(this.blobPreviewUrl);
        }
    },
    methods: {
        async post() {
            this.loading = true;
            this.error = null;
            this.success = null;

            try {
                let res;
                // If we have a blob, we use the composite endpoint (multipart/form-data)
                if (this.imageBlob) {
                    const formData = new FormData();
                    formData.append('image', this.imageBlob, 'nowplaying_composite.jpg');
                    formData.append('altText', this.altText);
                    formData.append('text', this.text);
                    
                    res = await fetch('/api/history/post-composite', {
                        method: 'POST',
                        body: formData
                    });
                } 
                // Otherwise we use the standard posting endpoint (JSON with image URL)
                else {
                    res = await fetch('/api/posting/post', {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify({
                            text: this.text,
                            imageUrl: this.imageUrl,
                            altText: this.altText
                        })
                    });
                }

                if (!res.ok) {
                    if (res.status === 401) {
                        this.$emit('unauthorized');
                        return;
                    }
                    throw new Error((await res.json()).error);
                }
                
                const data = await res.json();
                this.success = `Posted successfully! <a href="${data.url}" target="_blank">View Post</a>`;
                this.$emit('posted', data);
            } catch (e) {
                this.error = e.message;
            } finally {
                this.loading = false;
            }
        }
    }
}
