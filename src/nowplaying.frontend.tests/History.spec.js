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

    // Composite response now returns cacheId and contentType
    global.fetch.mockResolvedValueOnce({
        ok: true,
        json: async () => ({ cacheId: 'cached-123', contentType: 'image/png' })
    });

    wrapper = mount(History);
    await wrapper.vm.search();
    await wrapper.vm.$nextTick(); // Wait for composite generation
    await wrapper.vm.$nextTick(); // Wait for state update

    // verify component state was updated correctly
    expect(wrapper.vm.compositeCacheId).toBe('cached-123');
    expect(wrapper.vm.compositeUrl).toBe('/api/history/composite-preview/cached-123');

    // The "Share to Mastodon" button is shown when compositeUrl is present
    const shareBtn = wrapper.findAll('button').find(b => b.text().includes('Share to Mastodon'));
    expect(shareBtn).toBeDefined();
    await shareBtn.trigger('click');

    expect(wrapper.vm.showPostForm).toBe(true);
    expect(wrapper.findComponent({ name: 'MastodonPost' }).exists()).toBe(true);
  });

  it('resets search state after posting so no empty-result message displays', async () => {
    // simulate a completed search
    wrapper = mount(History);
    wrapper.vm.searched = true;
    wrapper.vm.posts = [{ imageUrl: 'foo', altText: 'bar' }];

    wrapper.vm.handlePosted();

    expect(wrapper.vm.searched).toBe(false);
    // message from template should not appear
    expect(wrapper.text()).not.toContain('No posts found in this range');
  });
});
