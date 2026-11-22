
import type { ViteDevServer } from 'vite';

let viteServer: ViteDevServer | undefined;

const dependencies = new Set<string>();

export function setViteServer(server: ViteDevServer) {

    viteServer = server;

    for (const file of dependencies) {
        viteServer.watcher.add(file);
    }
}

export function registerCsharpDependency(filename: string) {

    dependencies.add(filename);

    viteServer?.watcher.add(filename);
}
