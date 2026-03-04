import { describe, it, expect, beforeEach, vi } from 'vitest';
import { mount } from '@vue/test-utils';
import App from '../nowplaying/wwwroot/js/App.js';

describe('App Component', () => {
  let wrapper;

  beforeEach(() => {
    global.fetch = vi.fn();
  });

  it('renders the app with header and navigation', () => {
    global.fetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({ authenticated: false, registered: false, instance: null })
    });

    wrapper = mount(App);
    expect(wrapper.find('h1').text()).toContain('NowPlaying');
  });

  it('initializes with unauthenticated state', () => {
    global.fetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({ authenticated: false, registered: false, instance: null })
    });

    wrapper = mount(App);

    expect(wrapper.vm.authenticated).toBe(false);
    expect(wrapper.vm.registered).toBe(false);
    expect(wrapper.vm.instance).toBeNull();
  });

  it('computes instance name from URL', () => {
    wrapper = mount(App);
    wrapper.vm.instance = 'https://fosstodon.org';

    expect(wrapper.vm.instanceName).toBe('fosstodon.org');
  });

  it('strips protocol from instance name', () => {
    wrapper = mount(App);
    wrapper.vm.instance = 'https://mastodon.social';

    expect(wrapper.vm.instanceName).toBe('mastodon.social');
  });

  it('switches between Post and Review views', async () => {
    global.fetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({ authenticated: true, registered: true, instance: 'https://mastodon.social' })
    });

    wrapper = mount(App);
    await wrapper.vm.$nextTick();

    expect(wrapper.vm.view).toBe('post');
    expect(wrapper.vm.viewComponent).toBe('Post');

    wrapper.vm.view = 'review';
    await wrapper.vm.$nextTick();
    expect(wrapper.vm.viewComponent).toBe('Review');
  });

  it('handles fetch error gracefully', async () => {
    global.fetch.mockRejectedValueOnce(new Error('Network error'));
    const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {});

    wrapper = mount(App);
    await wrapper.vm.checkAuth();

    expect(consoleSpy).toHaveBeenCalled();
    consoleSpy.mockRestore();
  });
});
