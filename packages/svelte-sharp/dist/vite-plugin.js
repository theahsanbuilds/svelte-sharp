import { resolve } from 'node:path';
import { normalizePath } from 'vite';
import { setViteServer } from './watcher.js';
export function svelteSharp(_options = {}) {
    return {
        name: 'svelte-sharp',
        enforce: 'pre',
        configureServer(server) {
            setViteServer(server);
        },
        handleHotUpdate({ file, server }) {
            if (!file.endsWith('.svelte.cs')) {
                return;
            }
            const svelteFile = normalizePath(resolve(file.slice(0, -3)));
            const modules = server.moduleGraph.getModulesByFile(svelteFile);
            if (!modules || modules.size === 0) {
                return;
            }
            for (const module of modules) {
                server.moduleGraph.invalidateModule(module);
            }
            return [...modules];
        }
    };
}
