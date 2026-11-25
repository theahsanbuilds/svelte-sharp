# SvelteSharp

### Write C#. Build with Svelte.

**SvelteSharp** is an experimental C# component authoring toolkit for **Svelte 5 and SvelteKit**.

[🚀 Live Playground](https://theahsanbuilds.github.io/svelte-sharp/) · [npm Package](https://www.npmjs.com/package/svelte-sharp) · [GitHub Repository](https://github.com/theahsanbuilds/svelte-sharp)

It allows developers to write component logic using familiar C# syntax while using Svelte for rendering, reactivity, and DOM updates.

The goal is to bring a C# and Blazor-inspired development experience to the Svelte ecosystem without requiring developers to manually write component logic in JavaScript or TypeScript.

## Features

- **C# Component Logic** — Write component logic using familiar C# syntax.
- **Separate Component Files** — Use `.svelte.cs` companion files.
- **Inline C# Code** — Write component logic inside `@code { }` blocks.
- **Native Svelte Markup** — Keep your UI in standard `.svelte` files.
- **Automatic Reactive State** — Generate Svelte 5 `$state`.
- **Computed Properties** — Translate supported C# computed properties into `$derived`.
- **C# Methods** — Translate supported C# methods into JavaScript functions.
- **Two-Way Binding** — Use Svelte's native `bind:value`.
- **Component Parameters** — Support component parameters.
- **Independent Component Instances** — Maintain independent state across component instances.
- **Razor-Style Conditional Rendering** — Use `@if`, `else if`, and `else`.
- **Razor-Style Loops** — Use `@foreach` and supported `@for` patterns.
- **Razor-Style Switch Statements** — Use `@switch`, `case`, and `default`.
- **Nested Control Flow** — Combine supported conditional statements and loops.
- **Markup Parsing** — Parse Razor-style directives into an intermediate representation.
- **Svelte Code Generation** — Translate supported control-flow syntax into native Svelte blocks.
- **Syntax Validation** — Detect invalid C# syntax.
- **Semantic Validation** — Detect errors in supported C# constructs using Roslyn.
- **Compiler Diagnostics** — Report source locations, diagnostic codes, severity, and error messages.
- **Compilation Caching** — Cache successfully generated component output.
- **Vite Integration** — Integrate with the SvelteKit development workflow.
- **Local Compilation** — Execute the bundled C# compiler locally.

## Requirements

- Node.js
- npm
- Svelte 5
- SvelteKit
- Vite 6 or newer
- Windows x64 (current compiler distribution)

**Note:** The current compiler is distributed as a self-contained executable with its required .NET runtime files. Users do not need to install the .NET SDK or runtime separately.

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

When both inline and companion C# files are used, SvelteSharp combines their component logic and checks for duplicate member names.

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

SvelteSharp translates supported C# component logic into Svelte-compatible JavaScript during preprocessing. Svelte then handles rendering and reactive updates.

## Razor-Style Markup Control Flow

SvelteSharp supports a Razor-inspired syntax for conditional rendering and loops directly inside `.svelte` components.

### Conditional Rendering

Use `@if`, `else if`, and `else`:

```razor
@if (count > 10)
{
    <h2>Greater than 10</h2>
}
else if (count == 10)
{
    <h2>Exactly 10</h2>
}
else
{
    <h2>Less than 10</h2>
}
```

### Foreach Loops

Use `@foreach` to render collections:

```razor
@foreach (var product in products)
{
    <p>{product.Name}</p>
}
```

### For Loops

Use supported C#-style `@for` loops:

```razor
@for (int i = 0; i < 5; i++)
{
    <p>Number: {i}</p>
}
```

### Switch Statements

Use `@switch` for conditional branches:

```razor
@switch (status)
{
    case 1:
        <p>Waiting for approval</p>
        break;

    case 2:
        <p>Approved</p>
        break;

    default:
        <p>Unknown status</p>
        break;
}
```

### Nested Control Flow

Supported control-flow blocks can be nested:

```razor
@foreach (var product in products)
{
    @if (product.Price > 100)
    {
        <p>{product.Name} is expensive.</p>
    }
}
```

### Svelte Translation

SvelteSharp translates supported Razor-style control flow into native Svelte syntax.

| SvelteSharp | Generated Svelte |
|---|---|
| `@if` | `{#if}` |
| `else if` | `{:else if}` |
| `else` | `{:else}` |
| `@foreach` | `{#each}` |
| `@for` | `{#each}` |
| `@switch` | Conditional rendering blocks |

Svelte remains responsible for rendering, reactive updates, and DOM management.

**Note:** Control-flow support is experimental. Only implemented directives and supported loop patterns are available. Full C# and Razor compatibility is not provided.

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

SvelteSharp combines a .NET compiler, Roslyn semantic analysis, a markup parser, and a Svelte translation pipeline.

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
              Markup Parser
                 |
                 v
       Intermediate Representation
               (RazorNode)
                 |
                 v
        Svelte Markup Translator
                 |
                 v
           Generated Svelte
                 |
                 v
           Svelte 5 Compiler
                 |
                 v
               Browser
```

The markup parser recognizes supported Razor-style directives and converts them into an intermediate representation.

The markup translator processes these nodes and generates corresponding Svelte syntax.

This separates parsing, intermediate representation, and code generation, making it easier to expand the supported markup language.

SvelteSharp handles C# source processing, validation, and translation.

Svelte remains responsible for rendering, reactivity, and DOM updates.

**SvelteSharp is not a replacement for Svelte or Blazor.** It is a C# authoring and translation layer for the Svelte ecosystem.

## Current Limitations

SvelteSharp is an experimental project. It does not support the complete C# language or the entire .NET platform.

Current limitations include:

- Limited C# language support.
- Windows x64 compiler distribution only.
- No full .NET runtime execution in the browser.
- Not a drop-in replacement for Blazor.
- Some advanced C# and Blazor features are not implemented.
- Semantic validation is limited to supported C# constructs.
- Complete validation of C# members referenced from Svelte markup is not yet implemented.
- Advanced Razor functionality, templates, sections, and tag helpers are not currently supported.
- Advanced C# loop and switch patterns may not be supported.

## Roadmap

- [x] Separate `.svelte.cs` component files.
- [x] Inline `@code` blocks.
- [x] Automatic reactive state generation.
- [x] Computed properties.
- [x] C# methods.
- [x] Basic syntax validation.
- [x] Semantic validation for supported C# constructs.
- [x] Compilation caching.
- [x] Razor-style `@if` and `else` blocks.
- [x] Razor-style `@foreach` loops.
- [x] Supported Razor-style `@for` loops.
- [x] Razor-style `@switch` statements.
- [x] Parser-based markup translation.
- [x] Intermediate representation for markup control flow.
- [ ] Complete markup-to-C# member validation.
- [ ] Simplified project initialization.
- [ ] More C# language features.
- [ ] Component lifecycle integration.
- [ ] Dependency injection.
- [ ] Component communication.
- [ ] Improved Blazor and Razor compatibility.
- [ ] Blazor migration tooling.
- [ ] Linux and macOS support.

## Project Status

SvelteSharp is an experimental project under active development.

APIs, compiler behavior, and supported C# syntax may change between releases.

Feedback, bug reports, suggestions, and contributions are welcome.

## Links

- **Live Playground:** https://theahsanbuilds.github.io/svelte-sharp/
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