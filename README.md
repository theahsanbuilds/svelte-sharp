# SvelteSharp

SvelteSharp is an experimental tool for authoring Svelte components with C#.

## Repository layout

- `compiler/SvelteSharp.Compiler/`: .NET compiler source.
- `packages/svelte-sharp/`: reusable TypeScript npm integration package.
- `src/` and `static/`: SvelteKit development playground.

The compiler and npm package are intended to be independent of the playground.

## Current status

This ZIP contains the project structure and starter placeholders only. It does not contain your existing Roslyn translator implementation. Copy your existing compiler source files into `compiler/SvelteSharp.Compiler/` before attempting to build the compiler.
