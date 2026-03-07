// Copyright (c) Ben Hughes. SPDX-License-Identifier: AGPL-3.0-or-later
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { mount } from '@vue/test-utils';
import History from '../nowplaying/wwwroot/js/components/History.js';

describe('History Component', () => {
  let wrapper;

  beforeEach(() => {
    global.fetch = vi.fn();
    if (!global.URL) global.URL = {};
    global.URL.createObjectURL = vi.fn(() => 'blob:url');
    global.URL.revokeObjectURL = vi.fn();
    Element.prototype.scrollIntoView = vi.fn();
  });

  it('renders search form by default', () => {
    wrapper = mount(History);
    expect(wrapper.text()).toContain('Review History');
    expect(wrapper.find('input[type="date"]').exists()).toBe(true);
  });

  it('searches and shows results', async () => {
    global.fetch.mockResolvedValueOnce({
        ok: true,
        json: async () => ([
            { postId: '1', imageUrl: 'img1.jpg', altText: 'Alt 1' }
        ])
    });

    wrapper = mount(History);
    wrapper.vm.since = '2025-01-01';
    wrapper.vm.until = '2025-01-07';

    await wrapper.vm.search();

    expect(global.fetch).toHaveBeenCalledWith(
        '/api/history/search?since=2025-01-01&until=2025-01-07&tag=%23nowplaying'
    );
    expect(wrapper.vm.posts).toHaveLength(1);
  });

  it('generates composite and shows MastodonPost when Share clicked', async () => {
    // Search response
    global.fetch.mockResolvedValueOnce({
        ok: true,
        json: async () => ([{ imageUrl: 'img.jpg', altText: 'Alt' }])
    });

    // Composite response
    const blob = new Blob(['img'], { type: 'image/jpeg' });
    global.fetch.mockResolvedValueOnce({
        ok: true,
        blob: async () => blob
    });

    wrapper = mount(History);
    await wrapper.vm.search();
    await wrapper.vm.$nextTick(); // Wait for composite generation
    await wrapper.vm.$nextTick(); // Wait for state update

    expect(wrapper.vm.compositeBlob).toBeTruthy();
    
    // Set compositeUrl manually since we don't have a real DOM URL.createObjectURL
    wrapper.vm.compositeUrl = 'blob:url';
    await wrapper.vm.$nextTick();

    // The "Share to Mastodon" button is shown when compositeUrl is present
    const shareBtn = wrapper.findAll('button').find(b => b.text().includes('Share to Mastodon'));
    expect(shareBtn).toBeDefined();
    await shareBtn.trigger('click');

    expect(wrapper.vm.showPostForm).toBe(true);
    expect(wrapper.findComponent({ name: 'MastodonPost' }).exists()).toBe(true);
  });
});
