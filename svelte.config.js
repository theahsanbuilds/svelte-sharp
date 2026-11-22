
import adapter from '@sveltejs/adapter-auto';
import { vitePreprocess } from '@sveltejs/vite-plugin-svelte';
import { resolve } from 'node:path';
import { svelteSharpPreprocessor } from 'svelte-sharp';

const config = {
    preprocess: [
        svelteSharpPreprocessor(),
        vitePreprocess()
    ],

    kit: {
        adapter: adapter()
    }
};

export default config;
