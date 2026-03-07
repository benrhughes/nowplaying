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

  it('posts FormData payload when blob provided', async () => {
    global.fetch.mockResolvedValueOnce({
        ok: true,
        json: async () => ({ url: 'https://mastodon.social/123' })
    });

    const blob = new Blob(['test'], { type: 'image/jpeg' });
    wrapper = mount(MastodonPost, {
        props: {
            initialText: 'Text',
            imageBlob: blob
        }
    });

    await wrapper.vm.post();

    expect(global.fetch).toHaveBeenCalledWith(
        '/api/history/post-composite',
        expect.objectContaining({
            method: 'POST',
            body: expect.any(FormData)
        })
    );
    expect(wrapper.emitted('posted')).toBeTruthy();
  });
});
