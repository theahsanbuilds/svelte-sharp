
import { defineConfig } from 'vite';
import { svelte } from '@sveltejs/vite-plugin-svelte';

export default defineConfig({
    plugins: [svelte()],
    publicDir: 'static',
    base: '/svelte-sharp/',
    build: {
        target: 'es2022'
    }
});
