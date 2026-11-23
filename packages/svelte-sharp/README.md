# SvelteSharp

**Write C#. Build with Svelte.**

SvelteSharp is an experimental C# component authoring toolkit for **Svelte 5 and SvelteKit**.

It allows developers to write component logic using familiar C# syntax while using Svelte for rendering, reactivity, and DOM updates.

The goal is to bring a C# and Blazor-inspired development experience to the Svelte ecosystem without requiring developers to manually write component logic in JavaScript or TypeScript.

## Features

- Write component logic in C#.
- Use separate `.svelte.cs` files or inline `@code` blocks.
- Keep markup in native `.svelte` files.
- Automatic reactive state generation using Svelte 5 `$state`.
- Computed properties using `$derived`.
- C# methods translated into JavaScript functions.
- Native Svelte two-way binding.
- Component parameters.
- Independent state for multiple component instances.
- C# syntax and semantic validation.
- Compile-time diagnostics for supported C# code.
- Compilation caching.
- Vite development integration.
- Local C# compiler execution.

## Requirements

- Node.js
- npm
- Svelte 5
- SvelteKit
- Vite 6 or newer
- Windows x64 (current compiler distribution)

**Note:** The current compiler is distributed as a self-contained executable with its required .NET runtime files. Users do not need to install .NET separately.

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

SvelteSharp supports two ways to write C# component logic.

### Option 1: Separate C# File

Create two files next to each other.

**1. Counter.svelte.cs**

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

**2. Counter.svelte**

Write your markup using native Svelte syntax:

```svelte
<div class="counter">
    <h2>My C# Counter</h2>

    <h3>Count: {count}</h3>

    <p>{Message}</p>

    <button onclick={Increment}>+</button>
    <button onclick={Decrement}>-</button>
</div>
```

### Option 2: Inline C# Code

You can also write C# directly inside your `.svelte` component using `@code`.

**Counter.svelte**

```svelte
<div class="counter">
    <h2>My C# Counter</h2>

    <h3>Count: {count}</h3>

    <p>{Message}</p>

    <button onclick={Increment}>+</button>
    <button onclick={Decrement}>-</button>
</div>

@code {
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

Both approaches are supported. Choose whichever fits your project.

## Use Your Component

Import your component into a SvelteKit page:

```svelte
<script lang="ts">
    import Counter from '$lib/Counter.svelte';
</script>

<h1>Welcome to SvelteSharp</h1>

<Counter />
```

## Run the Application

```bash
npm run dev
```

SvelteSharp translates your C# component logic into Svelte-compatible JavaScript during compilation. Svelte then handles rendering and reactive updates.

## Two-Way Binding

SvelteSharp works with Svelte's native binding system.

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

## Compile-Time Validation

SvelteSharp validates supported C# code before translating it.

For example, this component contains an undefined variable:

```csharp
@code {
    private int count = ss0;
}
```

Instead of allowing the generated JavaScript to fail later in the browser, SvelteSharp reports a compiler diagnostic:

```text
Counter.svelte(2,25):
SSW004: The name 'ss0' does not exist in the current context
```

Compilation stops when an error is detected.

SvelteSharp also provides a diagnostics mode for inspecting compiler diagnostics:

```powershell
SvelteSharp.Compiler.exe --diagnostics Counter.svelte
```

Semantic validation currently focuses on supported C# component members and expressions. It does not yet provide complete C# language validation or validate every Svelte markup reference.

## How It Works

```text
.svelte file + optional .svelte.cs file
                 |
                 v
         SvelteSharp Preprocessor
                 |
                 v
          C# Source Processing
                 |
                 v
          Syntax Validation
                 |
                 v
         Semantic Validation
                 |
                 v
          C# Code Translation
                 |
                 v
      Generated Svelte-Compatible Code
                 |
                 v
           Svelte 5 Compiler
                 |
                 v
              Browser
```

SvelteSharp handles C# source processing, validation, and translation.

Svelte remains responsible for rendering, reactivity, and DOM updates.

## Current Limitations

SvelteSharp is an experimental project. It does not support the complete C# language or the entire .NET platform.

Current limitations include:

- Limited C# language support.
- Windows x64 compiler distribution only.
- Not a replacement for the .NET runtime.
- Not a drop-in replacement for Blazor.
- Some advanced C# and Blazor features are not implemented.
- Semantic validation is limited to supported C# constructs.
- Complete validation of C# members referenced from Svelte markup is not yet implemented.

## Roadmap

- [x] Separate `.svelte.cs` component files.
- [x] Inline `@code` blocks.
- [x] Automatic reactive state generation.
- [x] Computed properties.
- [x] C# methods.
- [x] Basic syntax validation.
- [x] Semantic validation for supported C# constructs.
- [x] Compilation caching.
- [ ] Complete markup-to-C# member validation.
- [ ] Simplified project initialization.
- [ ] More C# language features.
- [ ] Component lifecycle integration.
- [ ] Dependency injection.
- [ ] Component communication.
- [ ] Improved Blazor and Razor compatibility.
- [ ] Blazor migration tooling.
- [ ] Linux and macOS support.

## Source Code

GitHub: https://github.com/theahsanbuilds/svelte-sharp

Bug reports, suggestions, and contributions are welcome.

## License

MIT License (subject to the license included with the package).