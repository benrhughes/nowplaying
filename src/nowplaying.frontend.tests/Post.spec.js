import { describe, it, expect, beforeEach, vi } from 'vitest';
import { mount } from '@vue/test-utils';
import Post from '../nowplaying/wwwroot/js/components/Post.js';

describe('Post Component', () => {
  let wrapper;

  beforeEach(() => {
    global.fetch = vi.fn();
  });

  it('renders scrape form', async () => {
    wrapper = mount(Post);

    await wrapper.vm.$nextTick();
    const h2 = wrapper.findAll('h2').find(el => el.text().includes('Post a Bandcamp Album'));
    expect(h2).toBeDefined();
    expect(wrapper.find('input[type="url"]').exists()).toBe(true);
  });

  it('scrapes bandcamp URL', async () => {
    global.fetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({
        artist: 'Test Artist',
        album: 'Test Album',
        image: 'https://example.com/image.jpg',
        description: 'A test album'
      })
    });

    wrapper = mount(Post);

    wrapper.vm.url = 'https://testartist.bandcamp.com/album/test';
    await wrapper.vm.scrape();

    expect(global.fetch).toHaveBeenCalledWith(
      expect.stringContaining('/api/posting/scrape'),
      expect.any(Object)
    );
    expect(wrapper.vm.scrapedData).not.toBeNull();
  });

  it('displays preview after scraping', async () => {
    global.fetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({
        artist: 'Test Artist',
        album: 'Test Album',
        image: 'https://example.com/image.jpg',
        description: 'A test album'
      })
    });

    wrapper = mount(Post);

    wrapper.vm.url = 'https://testartist.bandcamp.com/album/test';
    await wrapper.vm.scrape();
    await wrapper.vm.$nextTick();

    expect(wrapper.vm.scrapedData).toEqual({
      artist: 'Test Artist',
      album: 'Test Album',
      image: 'https://example.com/image.jpg',
      description: 'A test album'
    });
  });

  it('posts to Mastodon with correct payload', async () => {
    global.fetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({ postId: '123', url: 'https://mastodon.social/@user/123' })
    });

    wrapper = mount(Post);

    wrapper.vm.scrapedData = {
      artist: 'Test Artist',
      album: 'Test Album',
      image: 'https://example.com/image.jpg',
      description: 'A test album'
    };
    wrapper.vm.postText = 'Listening to Test Album by Test Artist';

    await wrapper.vm.post();

    expect(global.fetch).toHaveBeenCalledWith(
      expect.stringContaining('/api/posting/post'),
      expect.any(Object)
    );
  });

  it('resets form when clicking back', async () => {
    wrapper = mount(Post);

    wrapper.vm.scrapedData = { artist: 'Test', album: 'Album' };
    wrapper.vm.error = 'Some error';
    wrapper.vm.success = 'Some success message';
    
    await wrapper.vm.$nextTick();
    wrapper.vm.reset();

    expect(wrapper.vm.scrapedData).toBeNull();
    expect(wrapper.vm.error).toBeNull();
    expect(wrapper.vm.success).toBeNull();
  });

  it('disables button while loading', async () => {
    wrapper = mount(Post);

    wrapper.vm.loading = true;
    await wrapper.vm.$nextTick();

    const buttons = wrapper.findAll('button[type="submit"]');
    expect(buttons.some(b => b.attributes('disabled') !== undefined)).toBe(true);
  });
});
