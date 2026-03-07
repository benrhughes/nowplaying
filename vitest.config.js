// Copyright (c) Ben Hughes. SPDX-License-Identifier: AGPL-3.0-or-later
import { defineConfig } from 'vitest/config';
import vue from '@vitejs/plugin-vue';

export default defineConfig({
  plugins: [vue()],
  test: {
    globals: true,
    environment: 'happy-dom',
    coverage: {
      provider: 'v8',
      reporter: ['text', 'json', 'html'],
      include: ['src/nowplaying/wwwroot/js/**/*.js']
    }
  }
});
