# SvelteSharp Browser Playground

This folder is intentionally separate from the existing SvelteSharp package/compiler.

## Architecture

```text
Browser
  |
  +-- Svelte UI (Vite)
  |
  +-- .NET 9 WebAssembly compiler
  |      |
  |      +-- references ../compiler/SvelteSharp.Compiler
  |      +-- calls Translator.TranslateComponent
  |
  +-- Svelte 5 compiler
  |
  +-- generated Svelte -> browser preview
```

No API or backend is required after the static files are deployed.

## Prerequisites

- Node.js
- npm
- .NET 9 SDK
- .NET WebAssembly workload

Install the .NET workload:

```powershell
dotnet workload install wasm-tools
```

## Run locally

From this `playground` directory:

```powershell
npm install
npm run build
npm run dev
```

For a faster UI-only iteration, use:

```powershell
npm run dev
```

The compiler must already have been published to `static/compiler` for the Run button to work.

## GitHub Pages

The generated Vite `dist` folder is static. GitHub Actions can build the compiler and UI and publish `playground/dist` as the Pages artifact.
