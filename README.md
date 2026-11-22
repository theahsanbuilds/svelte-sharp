# SvelteSharp

### Write C#. Build with Svelte.

**SvelteSharp** is an experimental C# component authoring toolkit for [Svelte 5](https://svelte.dev/) and [SvelteKit](https://svelte.dev/docs/kit).

It allows developers to write component logic using familiar C# syntax while keeping Svelte responsible for rendering, reactivity, and DOM updates.

The goal is to bring a C# and Blazor-inspired development experience into the Svelte ecosystem — without requiring developers to write their component logic in JavaScript or TypeScript.

```csharp
public partial class Counter
{
    private int count = 0;

    private string Message => $"You clicked {count} times";

    private void Increment()
    {
        count++;
    }
}
```

Write your component logic in C#, use native Svelte markup, and let SvelteSharp handle the translation.

---

## Why SvelteSharp?

Svelte provides a powerful component system and compiler-driven reactivity. C# provides a familiar, strongly typed programming experience and a productive development environment.

SvelteSharp brings these approaches together.

Instead of manually implementing JavaScript event handlers and component logic, developers can write familiar C# fields, properties, and methods.

SvelteSharp translates supported C# component logic into JavaScript that Svelte can compile and execute.

## Features

- **C# Component Logic** — Write component logic using familiar C# syntax.
- **Native Svelte Markup** — Keep your UI in standard `.svelte` files.
- **Automatic Reactive State** — Component state is translated into Svelte 5 `$state`.
- **Computed Properties** — C# computed properties are translated using `$derived`.
- **C# Methods** — Translate supported C# methods into JavaScript functions.
- **Two-Way Binding** — Use native Svelte `bind:value`.
- **Component Parameters** — Support component parameters.
- **Independent Component Instances** — Maintain independent state across component instances.
- **Vite Integration** — Integrate with the Vite development workflow.
- **Local Compilation** — Compile C# component logic locally.

## Requirements

| Requirement | Version |
|---|---|
| Node.js | Supported Node.js version |
| npm | Compatible npm version |
| Svelte | Svelte 5 |
| SvelteKit | Compatible version |
| Vite | 6 or newer |
| Operating System | Windows x64 (current compiler distribution) |

**Note:** The current compiler is distributed as a self-contained executable. Users do not need to install the .NET SDK or runtime separately.

## Installation

Install SvelteSharp in your SvelteKit project:

```bash
npm install svelte-sharp
```

## Configuration

Configure SvelteSharp in your `vite.config.ts`:

```typescript
import adapter from '@sveltejs/adapter-auto';
import { sveltekit } from '@sveltejs/kit/vite';
import { defineConfig } from 'vite';

import {
    svelteSharp,
    svelteSharpPreprocessor
} from 'svelte-sharp';

export default defineConfig({
    plugins: [
        svelteSharp(),

        sveltekit({
            adapter: adapter(),
            preprocess: [
                svelteSharpPreprocessor()
            ]
        })
    ]
});
```

## Your First C# Component

SvelteSharp uses two files for a component:

- `.svelte.cs` — C# component logic.
- `.svelte` — Native Svelte markup.

### 1. Create Counter.svelte.cs

Write your component logic in C#:

```csharp
public partial class Counter
{
    private int count = 0;

    private string Message =>
        $"You clicked {count} times";

    private void Increment()
    {
        count++;
    }

    private void Decrement()
    {
        count--;
    }
}
```

### 2. Create Counter.svelte

Write your UI using native Svelte syntax:

```svelte
<div class="counter">
    <h2>My C# Counter</h2>

    <h3>Count: {count}</h3>

    <p>{Message}</p>

    <button onclick={Increment}>+</button>
    <button onclick={Decrement}>-</button>
</div>
```

### 3. Use Your Component

Import the component into a SvelteKit page:

```svelte
<script lang="ts">
    import Counter from '$lib/Counter.svelte';
</script>

<h1>Welcome to SvelteSharp</h1>

<Counter />
```

### 4. Run Your Application

```bash
npm run dev
```

SvelteSharp translates the C# component logic during compilation. Svelte then compiles the resulting component for the browser.

## Two-Way Binding

SvelteSharp uses Svelte's native binding functionality.

**Counter.svelte.cs**

```csharp
public partial class Counter
{
    private string Name = "Ahsan";
}
```

**Counter.svelte**

```svelte
<input bind:value={Name} />

<p>Hello {Name}</p>
```

You do not need to write a separate input event handler. Svelte manages the binding and reactive updates.

## How It Works

SvelteSharp combines a C# compiler with the existing Svelte compilation pipeline.

```text
       C# Component
        .svelte.cs
            |
            v
   SvelteSharp Compiler
       (.NET / Roslyn)
            |
            v
   Generated JavaScript
            |
            v
      Svelte 5 Compiler
            |
            v
      Generated Output
            |
            v
          Browser
```

**SvelteSharp is not a replacement for Svelte.**

It acts as a C# authoring and translation layer. Svelte remains responsible for component rendering, reactivity, and DOM updates.

## Architecture

The repository is organized into three main areas:

| Directory | Purpose |
|---|---|
| `compiler/SvelteSharp.Compiler/` | .NET and Roslyn-based C# compiler |
| `packages/svelte-sharp/` | Reusable npm package and Vite integration |
| `src/` and `static/` | SvelteKit development playground |

The compiler and npm package are designed to remain independent of the development playground.

## Current Limitations

SvelteSharp is experimental and does not implement the complete C# language or the entire .NET platform.

Current limitations include:

- Limited C# language support.
- Windows x64 compiler distribution.
- No full .NET runtime execution in the browser.
- Not a drop-in replacement for Blazor.
- Some advanced C# language features are not implemented.
- Advanced Blazor and Razor functionality is not currently supported.

## Roadmap

The following are development goals, not promises of existing functionality.

- [ ] Simplified project initialization.
- [ ] Improved compiler diagnostics.
- [ ] Compilation caching and performance improvements.
- [ ] Expanded C# language support.
- [ ] Component lifecycle integration.
- [ ] Dependency injection.
- [ ] Component communication.
- [ ] Improved Blazor and Razor compatibility.
- [ ] Blazor migration tooling.
- [ ] Linux support.
- [ ] macOS support.
- [ ] Improved developer tooling.

## Project Status

SvelteSharp is an experimental project under active development.

APIs, compiler behavior, and supported C# syntax may change between releases.

Feedback, bug reports, feature suggestions, and contributions are welcome.

## Links

- **npm:** https://www.npmjs.com/package/svelte-sharp
- **GitHub:** https://github.com/theahsanbuilds/svelte-sharp
- **Svelte:** https://svelte.dev/
- **SvelteKit:** https://svelte.dev/docs/kit

## Contributing

Contributions and ideas are welcome.

If you find a bug or have a feature suggestion:

1. Open a GitHub issue.
2. Describe the problem or proposed improvement.
3. Include a minimal example where possible.
4. Submit a pull request if you would like to contribute code.

## License

MIT License.

See the `LICENSE` file for details.
