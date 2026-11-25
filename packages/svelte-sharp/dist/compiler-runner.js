import { spawn } from 'node:child_process';
import { createHash } from 'node:crypto';
import { existsSync } from 'node:fs';
import { mkdir, readFile, stat, writeFile } from 'node:fs/promises';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
const packageDirectory = resolve(dirname(fileURLToPath(import.meta.url)), '..');
function getCompilerPath(options) {
    if (options.compilerPath) {
        return resolve(options.compilerPath);
    }
    if (process.platform !== 'win32' || process.arch !== 'x64') {
        throw new Error(`SvelteSharp currently supports Windows x64 only. ` +
            `Your platform is ${process.platform}-${process.arch}.`);
    }
    return resolve(packageDirectory, 'compiler', 'win-x64', 'SvelteSharp.Compiler.exe');
}
function createCacheKey(filename, componentCode, csharpCode, compilerModified, compilerSize) {
    return createHash('sha256')
        .update(filename)
        .update(componentCode)
        .update(csharpCode)
        .update(String(compilerModified))
        .update(String(compilerSize))
        .digest('hex');
}
export async function compileComponent(filename, options = {}, suppliedContent) {
    const absoluteFilename = resolve(filename);
    const csharpFilename = `${absoluteFilename}.cs`;
    const compilerPath = getCompilerPath(options);
    // Read Svelte component content.
    const componentCode = suppliedContent ??
        await readFile(absoluteFilename, 'utf8');
    // Read companion C# file if it exists.
    const csharpCode = existsSync(csharpFilename)
        ? await readFile(csharpFilename, 'utf8')
        : '';
    // Check compiler information.
    let compilerInfo;
    try {
        compilerInfo = await stat(compilerPath);
    }
    catch {
        throw new Error(`SvelteSharp compiler was not found: ${compilerPath}\n` +
            `Build the compiler using: npm run compiler:build`);
    }
    // Generate cache key.
    const cacheKey = createCacheKey(absoluteFilename, componentCode, csharpCode, compilerInfo.mtimeMs, compilerInfo.size);
    // Store cache inside the consuming project's node_modules.
    const cacheDirectory = resolve(process.cwd(), 'node_modules', '.cache', 'svelte-sharp');
    const cacheFile = resolve(cacheDirectory, `${cacheKey}.svelte`);
    // Return cached output if it exists.
    try {
        return await readFile(cacheFile, 'utf8');
    }
    catch {
        // Cache does not exist.
    }
    // Execute compiler.
    const generatedCode = await new Promise((resolvePromise, reject) => {
        const useDotnet = Boolean(options.dotnetCommand);
        const child = spawn(useDotnet ? options.dotnetCommand : compilerPath, useDotnet
            ? [compilerPath, absoluteFilename]
            : [absoluteFilename], {
            cwd: process.cwd(),
            stdio: ['ignore', 'pipe', 'pipe']
        });
        let stdout = '';
        let stderr = '';
        child.stdout.setEncoding('utf8');
        child.stderr.setEncoding('utf8');
        child.stdout.on('data', chunk => {
            stdout += chunk;
        });
        child.stderr.on('data', chunk => {
            stderr += chunk;
        });
        child.on('error', error => {
            reject(new Error(`Could not start SvelteSharp compiler: ${error.message}`));
        });
        child.on('close', code => {
            if (code === 0) {
                resolvePromise(stdout);
            }
            else {
                reject(new Error(stderr.trim() ||
                    `SvelteSharp compiler exited with code ${code}`));
            }
        });
    });
    // Save successful output in cache.
    await mkdir(cacheDirectory, { recursive: true });
    await writeFile(cacheFile, generatedCode, 'utf8');
    return generatedCode;
}
