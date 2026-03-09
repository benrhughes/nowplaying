// Copyright (c) Ben Hughes. SPDX-License-Identifier: AGPL-3.0-or-later
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { mount } from '@vue/test-utils';
import Bandcamp from '../nowplaying/wwwroot/js/components/Bandcamp.js';

describe('Bandcamp Component', () => {
  let wrapper;

  beforeEach(() => {
    global.fetch = vi.fn();
    if (!global.URL) global.URL = {};
    global.URL.createObjectURL = vi.fn(() => 'blob:url');
    global.URL.revokeObjectURL = vi.fn();
  });

  it('renders scrape form by default', () => {
    wrapper = mount(Bandcamp);
    expect(wrapper.text()).toContain('Post a Bandcamp Album');
    expect(wrapper.find('input[type="url"]').exists()).toBe(true);
  });

  it('scrapes and shows MastodonPost', async () => {
    global.fetch.mockResolvedValueOnce({
        ok: true,
        json: async () => ({
            artist: 'Artist',
            album: 'Album',
            image: 'img.jpg',
            url: 'http://bc.com'
        })
    });

    wrapper = mount(Bandcamp);
    wrapper.vm.url = 'http://bc.com';
    await wrapper.vm.scrape();

    expect(global.fetch).toHaveBeenCalledWith(
        '/api/posting/scrape',
        expect.anything()
    );
    expect(wrapper.vm.scrapedData).toBeTruthy();
    expect(wrapper.findComponent({ name: 'MastodonPost' }).exists()).toBe(true);
  });

  it('resets when MastodonPost emits cancel', async () => {
    wrapper = mount(Bandcamp);
    wrapper.vm.scrapedData = { artist: 'A' };
    await wrapper.vm.$nextTick();

    const postComponent = wrapper.findComponent({ name: 'MastodonPost' });
    postComponent.vm.$emit('cancel');
    await wrapper.vm.$nextTick();

    expect(wrapper.vm.scrapedData).toBeNull();
    expect(wrapper.text()).toContain('Post a Bandcamp Album');
  });

  it('shows error message on scrape failure', async () => {
    global.fetch.mockResolvedValueOnce({
        ok: false,
        json: async () => ({ error: 'Invalid URL' })
    });

    wrapper = mount(Bandcamp);
    wrapper.vm.url = 'not-a-url';
    await wrapper.vm.scrape();

    expect(wrapper.vm.error).toBe('Invalid URL');
    expect(wrapper.text()).toContain('Invalid URL');
  });

  it('emits unauthorized when MastodonPost emits unauthorized', async () => {
    wrapper = mount(Bandcamp);
    wrapper.vm.scrapedData = { artist: 'A' };
    await wrapper.vm.$nextTick();

    const postComponent = wrapper.findComponent({ name: 'MastodonPost' });
    postComponent.vm.$emit('unauthorized');

    expect(wrapper.emitted('unauthorized')).toBeTruthy();
  });

  it('resets after successful post', async () => {
    wrapper = mount(Bandcamp);
    wrapper.vm.scrapedData = { artist: 'A' };
    wrapper.vm.url = 'http://bc.com';
    await wrapper.vm.$nextTick();

    const postComponent = wrapper.findComponent({ name: 'MastodonPost' });
    postComponent.vm.$emit('posted');
    await wrapper.vm.$nextTick();

    expect(wrapper.vm.scrapedData).toBeNull();
    expect(wrapper.vm.url).toBe('');
  });

  it('updatePreview does nothing if no scrapedData', () => {
    wrapper = mount(Bandcamp);
    wrapper.vm.updatePreview();
    expect(wrapper.vm.altText).toBe('');
  });
});
