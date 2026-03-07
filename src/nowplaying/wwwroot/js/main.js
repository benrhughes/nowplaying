// Copyright (c) Ben Hughes. SPDX-License-Identifier: AGPL-3.0-or-later
import App from './App.js';

const { createApp } = window.Vue;

if (!window.Vue) {
    console.error('Vue is not loaded! Check index.html script tags.');
} else {
    const app = createApp(App);
    app.mount('#app');
}
