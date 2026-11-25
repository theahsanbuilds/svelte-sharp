
import { dotnet } from './_framework/dotnet.js';

console.log('[SvelteSharp] Initializing .NET WASM...');

const { getAssemblyExports, getConfig } = await dotnet.create();

const config = getConfig();

console.log('[SvelteSharp] Main assembly:', config.mainAssemblyName);

const exports = await getAssemblyExports(config.mainAssemblyName);

const compiler =
    exports.SvelteSharp.Playground.Compiler.PlaygroundCompiler;

if (!compiler || typeof compiler.Translate !== 'function') {
    throw new Error('PlaygroundCompiler.Translate export not found.');
}

globalThis.svelteSharpWasm = {
    translate: compiler.Translate
};

console.log('[SvelteSharp] Compiler ready!');

export function translate(svelteSource, csharpSource) {
    return compiler.Translate(svelteSource, csharpSource);
}
