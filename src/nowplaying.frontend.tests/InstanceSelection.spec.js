// Copyright (c) Ben Hughes. SPDX-License-Identifier: AGPL-3.0-or-later
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { mount } from '@vue/test-utils';
import InstanceSelection from '../nowplaying/wwwroot/js/components/InstanceSelection.js';

describe('InstanceSelection Component', () => {
    let wrapper;

    beforeEach(() => {
        global.fetch = vi.fn();
    });

    it('renders the instance selection form', () => {
        wrapper = mount(InstanceSelection);
        expect(wrapper.find('h2').text()).toBe('Select Your Mastodon Instance');
        expect(wrapper.find('input#instance').exists()).toBe(true);
        expect(wrapper.find('button[type="submit"]').text()).toBe('Continue');
    });

    it('submits registration successfully', async () => {
        const instance = 'mastodon.social';
        global.fetch.mockResolvedValueOnce({
            ok: true,
            json: async () => ({ instance: 'https://mastodon.social' })
        });

        wrapper = mount(InstanceSelection);
        await wrapper.find('input#instance').setValue(instance);
        await wrapper.find('form').trigger('submit');

        // Wait for async operations to complete
        await new Promise(resolve => setTimeout(resolve, 0));

        expect(global.fetch).toHaveBeenCalledWith('/auth/register', expect.objectContaining({
            method: 'POST',
            body: JSON.stringify({ instance: 'https://mastodon.social' })
        }));

        expect(wrapper.emitted('registered')).toBeTruthy();
        expect(wrapper.emitted('registered')[0]).toEqual(['https://mastodon.social']);
    });

    it('handles registration failure', async () => {
        global.fetch.mockResolvedValueOnce({
            ok: false,
            json: async () => ({ error: 'Invalid instance' })
        });

        wrapper = mount(InstanceSelection);
        await wrapper.find('input#instance').setValue('invalid');
        await wrapper.find('form').trigger('submit');

        // Wait for async operations to complete
        await new Promise(resolve => setTimeout(resolve, 0));
        await wrapper.vm.$nextTick();

        const errorMsg = wrapper.find('.message-error');
        expect(errorMsg.exists()).toBe(true);
        expect(errorMsg.text()).toBe('Invalid instance');
        expect(wrapper.vm.loading).toBe(false);
    });

    it('handles network error during registration', async () => {
        global.fetch.mockRejectedValueOnce(new Error('Network failure'));
        const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {});

        wrapper = mount(InstanceSelection);
        await wrapper.find('input#instance').setValue('mastodon.social');
        await wrapper.find('form').trigger('submit.prevent');

        await wrapper.vm.$nextTick();
        expect(wrapper.find('.message-error').text()).toBe('An error occurred. Please try again.');
        expect(consoleSpy).toHaveBeenCalled();
        consoleSpy.mockRestore();
    });

    it('normalizes instance URL by adding https://', async () => {
        global.fetch.mockResolvedValueOnce({
            ok: true,
            json: async () => ({ instance: 'https://mastodon.social' })
        });

        wrapper = mount(InstanceSelection);
        await wrapper.find('input#instance').setValue('mastodon.social');
        await wrapper.find('form').trigger('submit.prevent');

        expect(global.fetch).toHaveBeenCalledWith('/auth/register', expect.objectContaining({
            body: JSON.stringify({ instance: 'https://mastodon.social' })
        }));
    });

    it('does not add https:// if already present', async () => {
        global.fetch.mockResolvedValueOnce({
            ok: true,
            json: async () => ({ instance: 'http://localhost:3000' })
        });

        wrapper = mount(InstanceSelection);
        await wrapper.find('input#instance').setValue('http://localhost:3000');
        await wrapper.find('form').trigger('submit.prevent');

        expect(global.fetch).toHaveBeenCalledWith('/auth/register', expect.objectContaining({
            body: JSON.stringify({ instance: 'http://localhost:3000' })
        }));
    });
});
