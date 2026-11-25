let viteServer;
const dependencies = new Set();
export function setViteServer(server) {
    viteServer = server;
    for (const file of dependencies) {
        viteServer.watcher.add(file);
    }
}
export function registerCsharpDependency(filename) {
    dependencies.add(filename);
    viteServer?.watcher.add(filename);
}
