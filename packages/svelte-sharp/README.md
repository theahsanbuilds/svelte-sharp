# SvelteSharp

**Write C#. Build with Svelte.**

SvelteSharp is an experimental C# component authoring toolkit for SvelteKit and Svelte 5.

It allows developers to write component logic in familiar C# syntax while using Svelte for rendering, reactivity and DOM updates.

The goal is to bring the C# and Blazor developer experience into the Svelte ecosystem without requiring developers to write their component logic in JavaScript or TypeScript.

## Features

- Write component logic in C#.
- Keep markup in native `.svelte` files.
- Automatic reactive state generation using Svelte 5 `$state`.
- Computed properties using `$derived`.
- C# methods translated into JavaScript functions.
- Native Svelte two-way binding.
- Component parameters.
- Independent state for multiple component instances.
- Vite development integration.
- Local C# compiler execution.

## Requirements

- Node.js
- npm
- Svelte 5
- SvelteKit
- Vite 6 or newer
- Windows x64 (current compiler distribution)

**Note:** The current compiler is distributed as a self-contained executable, so users do not need to install .NET separately.

## Installation

Install SvelteSharp in your SvelteKit project:

```bash
npm install svelte-sharp
```

## Configuration

Add SvelteSharp to your `vite.config.ts`:

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

Create two files next to each other.

### 1. Counter.svelte.cs

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

### 2. Counter.svelte

Write your component markup using native Svelte syntax:

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

Import it into your SvelteKit page:

```svelte
<script lang="ts">
    import Counter from '$lib/Counter.svelte';
</script>

<h1>Welcome to SvelteSharp</h1>

<Counter />
```

### 4. Run the Application

```bash
npm run dev
```

Your C# component logic is translated into Svelte-compatible JavaScript during compilation.

## Two-Way Binding

SvelteSharp uses Svelte's native binding system.

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

You don't need to write a separate input event handler. Svelte handles the binding and reactive updates.

## How It Works

```text
.svelte.cs
    |
    v
SvelteSharp C# Compiler
    |
    v
Generated Svelte-compatible JavaScript
    |
    v
Svelte 5 Compiler
    |
    v
Browser
```

SvelteSharp translates C# component logic, while Svelte handles rendering, reactivity and DOM updates.

## Current Limitations

SvelteSharp is an experimental project and does not support the complete C# language or the entire .NET platform.

Current limitations include:

- Limited C# language support.
- Windows x64 compiler distribution only.
- Not a replacement for the .NET runtime.
- Not a drop-in replacement for Blazor.
- Some advanced C# and Blazor features are not implemented.

## Roadmap

- [ ] Simplified project initialization.
- [ ] Better compiler diagnostics.
- [ ] Improved compilation caching.
- [ ] More C# language features.
- [ ] Component lifecycle integration.
- [ ] Dependency injection.
- [ ] Component communication.
- [ ] Improved Blazor and Razor compatibility.
- [ ] Blazor migration tooling.
- [ ] Linux and macOS support.

## Source Code

GitHub: https://github.com/theahsanbuilds/svelte-sharp

Bug reports, suggestions and contributions are welcome.

## License

MIT License (subject to the license included with the package).