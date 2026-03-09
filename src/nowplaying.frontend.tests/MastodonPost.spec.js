// Copyright (c) Ben Hughes. SPDX-License-Identifier: AGPL-3.0-or-later
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { mount } from '@vue/test-utils';
import MastodonPost from '../nowplaying/wwwroot/js/components/MastodonPost.js';

describe('MastodonPost Component', () => {
  let wrapper;

  beforeEach(() => {
    global.fetch = vi.fn();
    if (!global.URL) global.URL = {};
    global.URL.createObjectURL = vi.fn(() => 'blob:url');
    global.URL.revokeObjectURL = vi.fn();
  });

  it('renders initial text', () => {
    wrapper = mount(MastodonPost, {
        props: {
            initialText: 'Hello',
            initialAltText: 'Alt',
            imageUrl: 'img.jpg'
        }
    });
    expect(wrapper.find('textarea').element.value).toBe('Hello');
    expect(wrapper.vm.altText).toBe('Alt');
  });

  it('posts JSON payload when no blob provided', async () => {
    global.fetch.mockResolvedValueOnce({
        ok: true,
        json: async () => ({ url: 'https://mastodon.social/123' })
    });

    wrapper = mount(MastodonPost, {
        props: {
            initialText: 'Text',
            imageUrl: 'img.jpg'
        }
    });

    await wrapper.vm.post();

    expect(global.fetch).toHaveBeenCalledWith(
        '/api/posting/post',
        expect.objectContaining({
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                text: 'Text',
                imageUrl: 'img.jpg',
                altText: ''
            })
        })
    );
    expect(wrapper.emitted('posted')).toBeTruthy();
  });

  it('posts JSON payload when cacheId provided', async () => {
    global.fetch.mockResolvedValueOnce({
        ok: true,
        json: async () => ({ url: 'https://mastodon.social/123' })
    });

    wrapper = mount(MastodonPost, {
        props: {
            initialText: 'Text',
            cacheId: 'cached-123',
            previewUrl: '/some/url'
        }
    });

    await wrapper.vm.post();

    expect(global.fetch).toHaveBeenCalledWith(
        '/api/history/post-composite',
        expect.objectContaining({
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ cacheId: 'cached-123', altText: '', text: 'Text' })
        })
    );
    expect(wrapper.emitted('posted')).toBeTruthy();
  });

  it('emits unauthorized when post returns 401', async () => {
    global.fetch.mockResolvedValueOnce({
        ok: false,
        status: 401
    });

    wrapper = mount(MastodonPost, {
        props: { initialText: 'Text', imageUrl: 'img.jpg' }
    });

    await wrapper.vm.post();

    expect(wrapper.emitted('unauthorized')).toBeTruthy();
  });

  it('shows error on post failure', async () => {
    global.fetch.mockResolvedValueOnce({
        ok: false,
        json: async () => ({ error: 'Post failed' })
    });

    wrapper = mount(MastodonPost, {
        props: { initialText: 'Text', imageUrl: 'img.jpg' }
    });

    await wrapper.vm.post();

    expect(wrapper.vm.error).toBe('Post failed');
    expect(wrapper.text()).toContain('Post failed');
  });

  it('disables button when alt text is over limit', async () => {
    wrapper = mount(MastodonPost, {
        props: { initialText: 'Text', imageUrl: 'img.jpg' }
    });
    wrapper.vm.altText = 'A'.repeat(1501);
    await wrapper.vm.$nextTick();

    expect(wrapper.vm.altTextCharCount).toBe(1501);
    const button = wrapper.find('button');
    expect(button.element.disabled).toBe(true);
    expect(wrapper.text()).toContain('Alt text exceeds 1500 character limit');
  });

  it('emits cancel on back click', async () => {
    wrapper = mount(MastodonPost, {
        props: { initialText: 'Text', imageUrl: 'img.jpg' }
    });
    await wrapper.find('button.secondary').trigger('click');
    expect(wrapper.emitted('cancel')).toBeTruthy();
  });

  it('uses previewUrl when provided', () => {
    wrapper = mount(MastodonPost, {
        props: {
            initialText: 'Text',
            imageUrl: 'img.jpg',
            previewUrl: 'preview.jpg'
        }
    });
    expect(wrapper.vm.displayPreviewUrl).toBe('preview.jpg');
    expect(wrapper.find('img').attributes('src')).toBe('preview.jpg');
  });
});
