let compilerPromise: Promise<any> | undefined;

export function loadSvelteSharpCompiler(): Promise<any> {
  if (compilerPromise) return compilerPromise;

  compilerPromise = (async () => {
    const url = new URL('compiler/main.js', document.baseURI).href;
    return import(/* @vite-ignore */ url);
  })();

  return compilerPromise;
}
