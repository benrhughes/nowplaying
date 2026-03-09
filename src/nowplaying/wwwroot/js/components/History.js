// Copyright (c) Ben Hughes. SPDX-License-Identifier: AGPL-3.0-or-later
import MastodonPost from './MastodonPost.js';

export default {
    components: { MastodonPost },
    template: `
        <div>
            <!-- Search Form -->
            <article v-if="!showPostForm">
                <hgroup>
                    <h2>Review History</h2>
                    <p>Search your posts by tag and date range. A composite image of all album covers posted in that range will be generated.</p>
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
                    <label>
                        Tag
                        <input type="text" v-model="tag">
                    </label>
                </div>
                <button class="btn-primary" @click="search" :aria-busy="searching" :disabled="searching || generating">
                    {{ searching ? 'Searching...' : 'Search ' + tag }}
                </button>
                <p v-if="error" class="message-error">{{ error }}</p>

                <article v-if="posts.length > 0 && generating" aria-busy="true" ref="generatingStatus">
                    Generating composite image...
                    <footer>Processing {{ posts.length }} albums. This may take a few moments.</footer>
                </article>
                
                <article v-if="posts.length > 0 && !generating && !compositeUrl">
                    <h3>Found {{ posts.length }} albums.</h3>
                </article>

                <article v-else-if="searched && posts.length === 0">
                    <p>No posts found in this range.</p>
                </article>

                <article v-if="compositeUrl && !showPostForm">
                    <h2>Composite Image</h2>
                    <figure class="text-center">
                        <img :src="compositeUrl" class="composite-image">
                    </figure>

                    <div>
                        <button @click="openPostForm">Share to Mastodon</button>
                    </div>

                    <details v-if="posts.length > 0">
                        <summary>View Albums ({{ posts.length }})</summary>
                        <ol>
                            <li v-for="(post, index) in posts" :key="post.postId">
                                {{ post.altText }}
                            </li>
                        </ol>
                    </details>
                </article>
            </article>

            <!-- Preview & Post -->
            <MastodonPost v-else
                ref="postForm"
                :initial-text="postText"
                :initial-alt-text="altText"
                :cache-id="compositeCacheId"
                :preview-url="compositeUrl"
                @posted="handlePosted"
                @cancel="showPostForm = false"
                @unauthorized="$emit('unauthorized')"
            />
        </div>
    `,
    data() {
        const end = new Date();
        const start = new Date();
        start.setDate(end.getDate() - 7);
        
        return {
            since: start.toISOString().split('T')[0],
            until: end.toISOString().split('T')[0],
            tag: '#nowplaying',
            searching: false,
            generating: false,
            error: null,
            posts: [],
            searched: false,
            compositeUrl: null,
            compositeCacheId: null,
            showPostForm: false
        }
    },
    computed: {
        postText() {
            return `${this.tag}list Review: ${this.since} to ${this.until}`;
        },
        altText() {
            return this.posts.map((p, i) => `${i + 1}. ${p.altText}`).join('\n');
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
                const encodedTag = encodeURIComponent(this.tag);
                const res = await fetch(`/api/history/search?since=${this.since}&until=${this.until}&tag=${encodedTag}`);
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
            await this.$nextTick();
            this.$refs.generatingStatus?.scrollIntoView({ behavior: 'smooth' });

            try {
                const imageUrls = this.posts.map(p => p.imageUrl);
                const res = await fetch('/api/history/composite', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ imageUrls })
                });

                if (!res.ok) throw new Error((await res.json()).error);

                const data = await res.json();
                this.compositeCacheId = data.cacheId;

                // Rather than fetching the bytes and creating a blob, we can simply point
                // the <img> tag at the preview endpoint and let the browser load it.
                this.compositeUrl = `/api/history/composite-preview/${data.cacheId}`;
            } catch (e) {
                this.error = e.message;
            } finally {
                this.generating = false;
            }
        },
        async openPostForm() {
            this.showPostForm = true;
            await this.$nextTick();
            const el = this.$refs.postForm.$el || this.$refs.postForm;
            el?.scrollIntoView({ behavior: 'smooth' });
        },
        handlePosted() {
            // After we successfully post the composite, clear all state related to
            // the previous search so the UI returns to its original form.  In
            // particular we reset `searched`; otherwise the empty `posts` array
            // would trigger the "No posts found in this range" banner which is
            // misleading after a post has been made.
            this.showPostForm = false;
            this.posts = [];
            this.compositeUrl = null;
            this.compositeCacheId = null;
            this.searched = false;
        }
    }
}
