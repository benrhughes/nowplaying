import Post from './components/Post.js';
import Review from './components/Review.js';
import InstanceSelection from './components/InstanceSelection.js';

export default {
    components: { Post, Review, InstanceSelection },
    template: `
        <div class="container">
            <header>
                <div style="display: flex; align-items: center; gap: 15px;">
                    <h1>NowPlaying</h1>
                    <nav v-if="authenticated">
                        <button @click="view = 'post'" :class="['btn', view === 'post' ? 'btn-primary' : 'btn-secondary']">Post</button>
                        <button @click="view = 'review'" :class="['btn', view === 'review' ? 'btn-primary' : 'btn-secondary']">Review</button>
                    </nav>
                </div>
                <div id="auth-status">
                    <span v-if="authenticated && instance">
                        Logged in to <a :href="instance" target="_blank" class="instance-link">{{ instanceName }}</a>
                    </span>
                    <a v-if="authenticated" href="/auth/logout" class="btn btn-secondary" style="margin-left: 10px;">Logout</a>
                </div>
            </header>

            <main>
                <div v-if="!authenticated" style="margin-top: 20px;">
                    <InstanceSelection 
                        v-if="!registered"
                        @registered="handleInstanceRegistered"
                    />
                    <div v-else class="card">
                        <h2>Ready to Login</h2>
                        <p>Instance registered: <strong>{{ instanceName }}</strong></p>
                        <a href="/auth/login" class="btn btn-primary">Login with Mastodon</a>
                    </div>
                </div>
                <keep-alive v-else>
                    <component :is="viewComponent" 
                        :authenticated="authenticated" 
                        :registered="registered"
                        @unauthorized="checkAuth"
                    />
                </keep-alive>
            </main>
        </div>
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
                const res = await fetch('/api/status');
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
