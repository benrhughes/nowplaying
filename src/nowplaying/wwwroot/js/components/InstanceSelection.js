/**
 * Component for selecting and registering a Mastodon instance.
 */
export default {
    template: `
        <div class="card">
            <h2>Select Your Mastodon Instance</h2>
            <p>Enter your Mastodon instance URL to continue:</p>
            <form @submit.prevent="registerInstance">
                <div class="form-group">
                    <label for="instance">Instance URL:</label>
                    <input 
                        id="instance"
                        v-model="instanceUrl" 
                        type="text" 
                        placeholder="e.g., mastodon.social"
                        required
                        @keydown.enter="registerInstance"
                    >
                </div>
                <div v-if="error" class="message error">{{ error }}</div>
                <button 
                    type="submit" 
                    class="btn btn-primary" 
                    :disabled="loading"
                >
                    {{ loading ? 'Registering...' : 'Continue' }}
                </button>
            </form>
        </div>
    `,
    emits: ['registered'],
    data() {
        return {
            instanceUrl: '',
            loading: false,
            error: null
        };
    },
    methods: {
        async registerInstance() {
            this.loading = true;
            this.error = null;

            try {
                let instance = this.instanceUrl.trim();
                
                // Add https:// if no protocol is specified
                if (!instance.startsWith('http')) {
                    instance = 'https://' + instance;
                }

                const response = await fetch('/api/register', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({ instance })
                });

                if (!response.ok) {
                    const errorData = await response.json();
                    this.error = errorData.error || 'Failed to register instance';
                    return;
                }

                const data = await response.json();
                this.$emit('registered', data.instance);
            } catch (e) {
                console.error('Instance registration failed:', e);
                this.error = 'An error occurred. Please try again.';
            } finally {
                this.loading = false;
            }
        }
    }
};
