
import adapter from '@sveltejs/adapter-static';
import { sveltekit } from '@sveltejs/kit/vite';
import {
    svelteSharp,
    svelteSharpPreprocessor
} from 'svelte-sharp';
import { defineConfig } from 'vite';

export default defineConfig({
    plugins: [
        svelteSharp(),
        sveltekit({
            adapter: adapter({
                fallback: '404.html'
            }),
            preprocess: [
                svelteSharpPreprocessor()
            ]
        })
    ]
});
