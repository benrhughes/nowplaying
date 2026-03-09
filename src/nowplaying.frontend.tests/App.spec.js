// Copyright (c) Ben Hughes. SPDX-License-Identifier: AGPL-3.0-or-later
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import App from '../nowplaying/wwwroot/js/App.js';

describe('App Component', () => {
  let wrapper;

  beforeEach(() => {
    global.fetch = vi.fn().mockResolvedValue({
      ok: true,
      json: async () => ({ authenticated: false, registered: false, instance: null })
    });
    
    // Reset localStorage
    localStorage.clear();

    // Reset document state
    document.documentElement.removeAttribute('data-theme');
    vi.spyOn(document.documentElement, 'setAttribute');
    vi.spyOn(document.documentElement, 'removeAttribute');
  });

  it('renders the app with header and navigation', () => {
    wrapper = mount(App);
    expect(wrapper.find('h1').text()).toContain('NowPlaying');
  });

  it('initializes with unauthenticated state', () => {
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

    expect(wrapper.vm.view).toBe('bandcamp');
    expect(wrapper.vm.viewComponent).toBe('Bandcamp');

    wrapper.vm.view = 'history';
    await wrapper.vm.$nextTick();
    expect(wrapper.vm.viewComponent).toBe('History');
  });

  it('handles fetch error gracefully', async () => {
    global.fetch.mockRejectedValueOnce(new Error('Network error'));
    const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => { });

    wrapper = mount(App);
    await wrapper.vm.checkAuth();

    expect(consoleSpy).toHaveBeenCalled();
    consoleSpy.mockRestore();
  });

  it('initializes theme from localStorage', () => {
    localStorage.setItem('picoPreferredColorScheme', 'dark');
    wrapper = mount(App);

    expect(wrapper.vm.theme).toBe('dark');
    expect(document.documentElement.setAttribute).toHaveBeenCalledWith('data-theme', 'dark');
  });

  it('sets theme and updates localStorage', () => {
    wrapper = mount(App);
    wrapper.vm.setTheme('light');

    expect(wrapper.vm.theme).toBe('light');
    expect(localStorage.getItem('picoPreferredColorScheme')).toBe('light');
    expect(document.documentElement.setAttribute).toHaveBeenCalledWith('data-theme', 'light');

    wrapper.vm.setTheme('auto');
    expect(wrapper.vm.theme).toBe('auto');
    expect(localStorage.getItem('picoPreferredColorScheme')).toBe('auto');
    expect(document.documentElement.removeAttribute).toHaveBeenCalledWith('data-theme');
  });

  it('handles unauthorized event by redirecting to logout', () => {
    // Mock window.location
    const originalLocation = window.location;
    delete window.location;
    window.location = { href: '' };

    wrapper = mount(App);
    wrapper.vm.handleUnauthorized();

    expect(window.location.href).toBe('/auth/logout');
    window.location = originalLocation;
  });

  it('updates state on handleInstanceRegistered', () => {
    wrapper = mount(App);
    wrapper.vm.handleInstanceRegistered('https://instance.com');

    expect(wrapper.vm.registered).toBe(true);
    expect(wrapper.vm.instance).toBe('https://instance.com');
  });

  it('shows login button when registered but not authenticated', async () => {
    global.fetch.mockResolvedValueOnce({
        ok: true,
        json: async () => ({ authenticated: false, registered: true, instance: 'https://instance.com' })
    });

    wrapper = mount(App);
    await flushPromises();

    expect(wrapper.vm.registered).toBe(true);
    expect(wrapper.text()).toContain('Ready to Login');
    expect(wrapper.find('a[href="/auth/login"]').exists()).toBe(true);
  });
});
