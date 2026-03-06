import Bandcamp from './components/Bandcamp.js';
import History from './components/History.js';
import InstanceSelection from './components/InstanceSelection.js';

export default {
    components: { Bandcamp, History, InstanceSelection },
    template: `
        <main class="container">
            <nav class="nav">
                <ul>
                    <li>
                        <hgroup>
                            <h1>NowPlaying</h1>
                            <h2 class="subtitle">
                                Share your music listening with 
                                <a v-if="registered && instance" :href="instance" target="_blank">{{ instanceName }}</a>
                                <span v-else>Mastodon</span>
                            </h2>
                        </hgroup>
                    </li>
                </ul>
                <ul>
                    <li>
                        <a v-if="authenticated" href="/auth/logout">Log out</a>
                    </li>
                    <li>
                        <select id="theme-select" v-model="theme" @change="setTheme(theme)">
                            <option value="auto">System</option>
                            <option value="light">Light</option>
                            <option value="dark">Dark</option>
                        </select>
                    </li>
                </ul>
            </nav>
            <nav>
                <ul>
                    <li v-if="authenticated"><a href="#" @click.prevent="view = 'bandcamp'" :aria-current="view === 'bandcamp' ? 'page' : null">Post a Bandcamp Album</a></li>
                    <li v-if="authenticated"><a href="#" @click.prevent="view = 'history'" :aria-current="view === 'history' ? 'page' : null">#NowPlaying History</a></li>
                </ul>
            </nav>

            <div v-if="!authenticated">
                <InstanceSelection 
                    v-if="!registered"
                    @registered="handleInstanceRegistered"
                />
                <article v-else>
                    <hgroup>
                        <h2>Ready to Login</h2>
                        <p>Instance registered: <strong>{{ instanceName }}</strong></p>
                    </hgroup>
                    <a href="/auth/login">Login with Mastodon</a>
                </article>
            </div>
            <keep-alive v-else>
                <component :is="viewComponent" 
                    ref="currentView"
                    :authenticated="authenticated" 
                    :registered="registered"
                    @unauthorized="handleUnauthorized"
                />
            </keep-alive>
        </main>
    `,
    data() {
        return {
            view: 'bandcamp',
            authenticated: false,
            registered: false,
            instance: null,
            theme: 'auto'
        }
    },
    computed: {
        viewComponent() {
            if (this.view === 'history') return 'History';
            return 'Bandcamp';
        },
        instanceName() {
            return this.instance ? this.instance.replace(/^https?:\/\//, '') : '';
        }
    },
    created() {
        this.checkAuth();
        this.initTheme();
    },
    methods: {
        initTheme() {
            const key = 'picoPreferredColorScheme';
            const stored = window.localStorage?.getItem(key) ?? 'auto';
            this._themeKey = key;
            this.setTheme(stored);
        },

        setTheme(scheme) {
            // scheme: 'light' | 'dark' | 'auto'
            this.theme = scheme;
            if (scheme === 'auto') {
                // remove explicit attribute to follow system
                document.documentElement.removeAttribute('data-theme');
                // store as 'auto' so we can keep preference
                window.localStorage?.setItem(this._themeKey, 'auto');
            } else {
                document.documentElement.setAttribute('data-theme', scheme);
                window.localStorage?.setItem(this._themeKey, scheme);
            }
        },

        async checkAuth() {
            try {
                const res = await fetch('/auth/status');
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
        },
        handleUnauthorized() {
            window.location.href = '/auth/logout';
        }
    }
}
