import { describe, it, expect, beforeEach, vi } from 'vitest';
import { mount } from '@vue/test-utils';
import Review from '../nowplaying/wwwroot/js/components/Review.js';

describe('Review Component', () => {
  let wrapper;

  beforeEach(() => {
    global.fetch = vi.fn();
    vi.clearAllMocks();
  });

  it('renders review search form', () => {
    wrapper = mount(Review);

    expect(wrapper.find('h2').text()).toContain('Review History');
    expect(wrapper.findAll('input[type="date"]')).toHaveLength(2);
    expect(wrapper.find('button').text()).toContain('Search #nowplaying');
  });

  it('sets default date range to last 7 days', () => {
    wrapper = mount(Review);

    const today = new Date();
    const weekAgo = new Date();
    weekAgo.setDate(today.getDate() - 7);

    const sinceDate = new Date(wrapper.vm.since);
    const untilDate = new Date(wrapper.vm.until);

    // Allow 1 day variance for execution time
    expect(untilDate - today).toBeLessThan(86400000);
    expect(sinceDate - weekAgo).toBeLessThanOrEqual(86400000);
  });

  it('searches for tagged posts', async () => {
    global.fetch.mockResolvedValueOnce({
      ok: true,
      json: async () => [
        {
          postId: '1',
          imageUrl: 'https://example.com/image1.jpg',
          description: 'Album 1'
        },
        {
          postId: '2',
          imageUrl: 'https://example.com/image2.jpg',
          description: 'Album 2'
        }
      ]
    });

    wrapper = mount(Review);

    wrapper.vm.since = '2025-01-01';
    wrapper.vm.until = '2025-01-31';
    await wrapper.vm.search();

    const expectedUrl = '/api/history/search?since=2025-01-01&until=2025-01-31';
    expect(global.fetch).toHaveBeenCalledWith(expectedUrl);
    expect(wrapper.vm.posts).toHaveLength(2);
    expect(wrapper.vm.searched).toBe(true);
  });

  it('displays search results', async () => {
    global.fetch.mockResolvedValueOnce({
      ok: true,
      json: async () => [
        {
          postId: '1',
          imageUrl: 'https://example.com/image1.jpg',
          altText: 'Album 1',
          description: 'Album 1'
        }
      ]
    });

    global.fetch.mockResolvedValueOnce({
      ok: true,
      blob: async () => new Blob(['image data'], { type: 'image/jpeg' })
    });

    wrapper = mount(Review);
    await wrapper.vm.search();
    await wrapper.vm.$nextTick();
    await wrapper.vm.$nextTick();

    expect(wrapper.text()).toContain('Albums (1)');
    expect(wrapper.vm.compositeUrl).toBeTruthy();
  });

  it('hides spinner on search button when composite is generating', async () => {
    wrapper = mount(Review);
    wrapper.vm.searching = false;
    wrapper.vm.generating = true;
    await wrapper.vm.$nextTick();

    const button = wrapper.find('button.btn-primary');
    expect(button.text()).toContain('Search #nowplaying');
    expect(button.find('.loading').exists()).toBe(false);
    expect(button.attributes('disabled')).toBeDefined();
  });

  it('shows "no posts found" message when search returns empty', async () => {
    global.fetch.mockResolvedValueOnce({
      ok: true,
      json: async () => []
    });

    wrapper = mount(Review);
    await wrapper.vm.search();
    await wrapper.vm.$nextTick();

    expect(wrapper.text()).toContain('No posts found in this range');
  });

  it('emits unauthorized event on 401 response', async () => {
    global.fetch.mockResolvedValueOnce({
      ok: false,
      status: 401
    });

    wrapper = mount(Review);
    await wrapper.vm.search();

    expect(wrapper.emitted('unauthorized')).toBeTruthy();
  });

  it('handles search error', async () => {
    global.fetch.mockResolvedValueOnce({
      ok: false,
      json: async () => ({ error: 'Server error' })
    });

    wrapper = mount(Review);
    await wrapper.vm.search();

    expect(wrapper.vm.error).toContain('Server error');
  });

  it('generates composite image from posts', async () => {
    const mockBlob = new Blob(['image data'], { type: 'image/jpeg' });
    global.fetch.mockResolvedValueOnce({
      ok: true,
      blob: async () => mockBlob
    });

    wrapper = mount(Review);
    wrapper.vm.posts = [
      { postId: '1', imageUrl: 'https://example.com/image1.jpg', description: 'Album 1' },
      { postId: '2', imageUrl: 'https://example.com/image2.jpg', description: 'Album 2' }
    ];

    await wrapper.vm.generateComposite();

    expect(global.fetch).toHaveBeenCalledWith(
      '/api/history/composite',
      {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          imageUrls: [
            'https://example.com/image1.jpg',
            'https://example.com/image2.jpg'
          ]
        })
      }
    );
    expect(wrapper.vm.compositeUrl).toBeTruthy();
  });

  it('displays composite image after generation', async () => {
    const mockBlob = new Blob(['image data'], { type: 'image/jpeg' });
    global.fetch.mockResolvedValueOnce({
      ok: true,
      blob: async () => mockBlob
    });

    wrapper = mount(Review);
    wrapper.vm.posts = [{ postId: '1', imageUrl: 'https://example.com/image1.jpg', altText: 'Album 1', description: 'Album 1' }];

    await wrapper.vm.generateComposite();
    await wrapper.vm.$nextTick();

    expect(wrapper.text()).toContain('Composite Image');
    expect(wrapper.find('img.composite-image').exists()).toBe(true);
    const shareButtons = wrapper.findAll('button');
    const shareButton = shareButtons.find(btn => btn.text().includes('Share to Mastodon'));
    expect(shareButton).toBeDefined();
  });

  it('handles composite generation error', async () => {
    global.fetch.mockResolvedValueOnce({
      ok: false,
      json: async () => ({ error: 'Image generation failed' })
    });

    wrapper = mount(Review);
    wrapper.vm.posts = [{ postId: '1', imageUrl: 'https://example.com/image1.jpg', description: 'Album 1' }];

    await wrapper.vm.generateComposite();

    expect(wrapper.vm.error).toContain('Image generation failed');
  });

  it('disables buttons while loading', async () => {
    wrapper = mount(Review);

    wrapper.vm.searching = true;
    await wrapper.vm.$nextTick();

    expect(wrapper.find('button:not([disabled])').exists()).toBe(false);
  });

  it('shows share button when composite is ready', async () => {
    wrapper = mount(Review);
    wrapper.vm.compositeUrl = 'blob:http://localhost/123';
    wrapper.vm.posts = [{ postId: '1', imageUrl: 'https://example.com/image1.jpg', altText: 'Album 1' }];

    await wrapper.vm.$nextTick();

    expect(wrapper.find('img.composite-image').exists()).toBe(true);
    const shareButtons = wrapper.findAll('button');
    const shareButton = shareButtons.find(btn => btn.text().includes('Share to Mastodon'));
    expect(shareButton).toBeDefined();
  });
});
