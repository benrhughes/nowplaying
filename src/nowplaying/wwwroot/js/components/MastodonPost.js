// Copyright (c) Ben Hughes. SPDX-License-Identifier: AGPL-3.0-or-later
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
                <img :src="displayPreviewUrl" :alt="altText" class="preview-image">
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
        previewUrl: String,
        cacheId: String
    },
    data() {
        return {
            text: this.initialText || '',
            altText: this.initialAltText || '',
            loading: false,
            error: null,
            success: null
        }
    },
    computed: {
        altTextCharCount() {
            return this.altText ? this.altText.length : 0;
        },
        displayPreviewUrl() {
            return this.previewUrl || this.imageUrl;
        }
    },
    methods: {
        async post() {
            this.loading = true;
            this.error = null;
            this.success = null;

            try {
                let res;
                // If we have a cache ID, we use the post-composite endpoint (cached image)
                if (this.cacheId) {
                    res = await fetch('/api/history/post-composite', {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify({
                            cacheId: this.cacheId,
                            altText: this.altText,
                            text: this.text
                        })
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
