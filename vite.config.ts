
import { resolve } from 'node:path';
import { sveltekit } from '@sveltejs/kit/vite';
import { svelteSharp } from 'svelte-sharp';
import { defineConfig } from 'vite';

export default defineConfig({
    plugins: [
        svelteSharp(),
        sveltekit()
    ]
});
