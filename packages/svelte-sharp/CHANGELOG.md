# Changelog

All notable changes to SvelteSharp are documented in this file.

## [0.1.2] - 2026-11-26

### Added

- Published the browser-based SvelteSharp Playground.
- Added .NET WebAssembly compiler support for in-browser C# to Svelte translation.
- Added a browser-based development environment for editing C# component logic and Svelte markup.
- Added live compiler translation and generated Svelte output display.
- Added local browser storage for Playground source persistence.

### Improved

- Improved project documentation and installation guidance.
- Added direct links to the Live Playground from project documentation.
- Improved separation between the compiler, npm package, and Playground deployment.

## [0.1.1] - 2026-11-24

### Added

- Razor-style conditional rendering using `@if`, `else if`, and `else`.
- Razor-style collection iteration using `@foreach`.
- Support for C#-style `@for` loops with supported loop patterns.
- Razor-style switch statements using `@switch`, `case`, and `default`.
- Nested control-flow blocks.
- Intermediate Representation (IR) using Razor node types.
- Dedicated Razor markup parser.
- Dedicated Razor markup translator.
- Translation of Razor-style conditional blocks into native Svelte `{#if}` blocks.
- Translation of Razor-style loops into native Svelte `{#each}` blocks.
- Translation of switch statements into Svelte conditional rendering.
- Support for HTML comments and basic script/style preservation during markup translation.
- Improved separation between markup parsing and code generation.

### Improved

- Replaced regex-based control-flow transformation with a parser-based approach.
- Improved handling of nested markup and control-flow blocks.
- Improved handling of `else if` and `else` branches.
- Improved compiler architecture for future language extensions.

## [0.1.0] - 2026-11-23

### Added

- Initial SvelteSharp npm package release.
- C# component authoring using `.svelte.cs` companion files.
- Support for inline `@code` blocks.
- Combining inline and companion C# component logic.
- Duplicate member detection across component sources.
- C# fields translated into Svelte 5 `$state`.
- Computed properties translated into Svelte 5 `$derived`.
- Supported C# methods translated into JavaScript functions.
- Native Svelte markup integration.
- Two-way binding support using Svelte bindings.
- Component parameter support.
- Independent component state across component instances.
- Roslyn-based C# syntax validation.
- Semantic validation for supported C# constructs.
- Compiler diagnostics with file paths, line numbers, columns, diagnostic codes, and severity.
- JSON diagnostics mode for developer tooling.
- Vite integration through a SvelteSharp preprocessor.
- Local C# compilation using the bundled compiler.
- Self-contained Windows x64 compiler distribution.
- Compilation output caching.
- SvelteKit playground for testing C# component translation.

### Added Developer Tooling

- Initial SvelteSharp VS Code extension.
- C# component diagnostics integration.
- VS Code Problems panel integration.
- Support for `.svelte.cs` and inline `@code` component sources.
- SvelteSharp compiler integration.

### Architecture

- Introduced a .NET-based compiler using Roslyn.
- Established C# to JavaScript translation.
- Integrated generated JavaScript and component logic into the Svelte compilation pipeline.
- Separated the compiler, npm package, and SvelteKit playground.

---

## Project Status

SvelteSharp is an experimental project under active development.

Compiler behavior, supported syntax, APIs, and package structure may change between releases.

See the [README](README.md) for installation instructions, supported features, architecture, and current limitations.

## Links

- [Live Playground](https://theahsanbuilds.github.io/svelte-sharp/)
- [GitHub Repository](https://github.com/theahsanbuilds/svelte-sharp)
- [npm Package](https://www.npmjs.com/package/svelte-sharp)
- [Svelte](https://svelte.dev/)
- [SvelteKit](https://svelte.dev/docs/kit)