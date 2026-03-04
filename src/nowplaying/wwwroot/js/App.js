import Post from './components/Post.js';
import Review from './components/Review.js';
import InstanceSelection from './components/InstanceSelection.js';

export default {
    components: { Post, Review, InstanceSelection },
    template: `
        <main class="container">
            <header class="app-header">
                <div class="header-top">
                    <h1>NowPlaying</h1>
                    <div id="auth-status">
                        <span v-if="authenticated && instance">
                            Logged in to <a :href="instance" target="_blank" class="instance-link">{{ instanceName }}</a>
                        </span>
                        <a v-if="authenticated" href="/auth/logout" role="button" class="secondary outline">Logout</a>
                    </div>
                </div>
                <div v-if="authenticated" class="tabs-wrapper">
                    <div class="tabs">
                        <a href="#" @click.prevent="view = 'post'" class="tab-link" :class="{ active: view === 'post' }">Post from Bandcamp</a>
                        <a href="#" @click.prevent="view = 'review'" class="tab-link" :class="{ active: view === 'review' }">#NowPlaying History</a>
                    </div>
                </div>
            </header>

            <div v-if="!authenticated">
                <InstanceSelection 
                    v-if="!registered"
                    @registered="handleInstanceRegistered"
                />
                <article v-else>
                    <header><strong>Ready to Login</strong></header>
                    <p>Instance registered: <strong>{{ instanceName }}</strong></p>
                    <a href="/auth/login" role="button" class="w-100">Login with Mastodon</a>
                </article>
            </div>
            <keep-alive v-else>
                <component :is="viewComponent" 
                    :authenticated="authenticated" 
                    :registered="registered"
                    @unauthorized="checkAuth"
                />
            </keep-alive>
        </main>
    `,
    data() {
        return {
            view: 'post',
            authenticated: false,
            registered: false,
            instance: null
        }
    },
    computed: {
        viewComponent() {
            if (this.view === 'review') return 'Review';
            return 'Post';
        },
        instanceName() {
            return this.instance ? this.instance.replace(/^https?:\/\//, '') : '';
        }
    },
    created() {
        this.checkAuth();
    },
    methods: {
        async checkAuth() {
            try {
                const res = await fetch('/api/config/status');
                const data = await res.json();
                this.authenticated = data.authenticated;
                this.registered = data.registered;
                this.instance = data.instance;
            } catch (e) {
                console.error('Auth check failed:', e);
            }
        },
        handleInstanceRegistered(instance) {
            this.registered = true;
            this.instance = instance;
        }
    }
}
