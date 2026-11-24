# SvelteSharp

### Write C#. Build with Svelte.

**SvelteSharp** is an experimental C# component authoring toolkit for [Svelte 5](https://svelte.dev/) and [SvelteKit](https://svelte.dev/docs/kit).

It allows developers to write component logic using familiar C# syntax while keeping Svelte responsible for rendering, reactivity, and DOM updates.

The goal is to bring a C# and Blazor-inspired development experience into the Svelte ecosystem without requiring developers to write their component logic in JavaScript or TypeScript.

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
- **Razor-Style Conditional Rendering** — Use `@if`, `else if`, and `else` in component markup.
- **Razor-Style Loops** — Use `@foreach` and supported `@for` loops.
- **Razor-Style Switch Statements** — Use `@switch`, `case`, and `default`.
- **Nested Control Flow** — Combine supported conditional statements and loops.
- **Markup Parsing** — Parse Razor-style directives into an intermediate representation before translation.
- **Svelte Code Generation** — Generate native Svelte control-flow blocks from supported Razor syntax.
- **Semantic Validation** — Detect supported C# semantic errors before generating component output.
- **Compiler Diagnostics** — Report C# errors with source file, line, column, and diagnostic codes.
- **Syntax Validation** — Detect invalid C# syntax during compilation.
- **Vite Integration** — Integrate with the Vite development workflow.
- **Local Compilation** — Compile C# component logic locally.
- **Compilation Caching** — Cache successfully generated component output.

## Requirements

| Requirement | Version |
|---|---|
| Node.js | Supported Node.js version |
| npm | Compatible npm version |
| Svelte | Svelte 5 |
| SvelteKit | Compatible version |
| Vite | 6 or newer |
| Operating System | Windows x64 (current compiler distribution) |

**Note:** The current compiler is distributed as a self-contained executable with its dependencies. Users do not need to install the .NET SDK or runtime separately.

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

SvelteSharp supports two ways to write component logic:

- `.svelte.cs` — Separate C# component logic.
- `@code { }` — Inline C# component logic inside a `.svelte` file.

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

SvelteSharp translates the C# component logic during preprocessing. Svelte then compiles the resulting component for the browser.

## Inline C# Code

You can also write C# directly inside your `.svelte` component using `@code`.

```svelte
@code {
    private int count = 0;

    private void Increment()
    {
        count++;
    }
}

<div class="counter">
    <h2>My C# Counter</h2>

    <p>Count: {count}</p>

    <button onclick={Increment}>+</button>
</div>
```

Both inline C# and companion `.svelte.cs` files are supported. When both are used, SvelteSharp combines their component logic and checks for duplicate member names.

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

## Razor-Style Markup Control Flow

SvelteSharp supports a Razor-inspired syntax for writing conditional rendering and loops directly inside `.svelte` components.

Instead of manually writing Svelte control-flow blocks, developers can use familiar C#-inspired markup syntax.

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

SvelteSharp translates these blocks into native Svelte conditional rendering.

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

### Native Svelte Compatibility

SvelteSharp translates supported Razor-style control flow into native Svelte syntax.

For example:

| SvelteSharp syntax | Generated Svelte syntax |
|---|---|
| `@if` | `{#if}` |
| `else if` | `{:else if}` |
| `else` | `{:else}` |
| `@foreach` | `{#each}` |
| `@for` | `{#each}` |
| `@switch` | Conditional rendering blocks |

Svelte remains responsible for rendering, reactive updates, and DOM management.

**Note:** Control-flow support is experimental. Only the supported syntax and loop patterns are currently implemented. Full C# and Razor language compatibility is not provided.


## Compiler Diagnostics and Semantic Validation

SvelteSharp uses Roslyn to analyze supported C# component code before translation.

It can detect errors such as:

- Undefined variables.
- Unknown types.
- Invalid assignments and conversions.
- Invalid method arguments.
- Invalid member access.
- Unsupported or incorrect expressions within analyzed component code.

For example, this component contains an undefined variable:

```csharp
@code {
    private int count = ss0;
}
```

SvelteSharp reports a diagnostic similar to:

```text
Counter.svelte(2,25):
SSW004: The name 'ss0' does not exist in the current context
```

Instead of generating JavaScript containing an undefined identifier, SvelteSharp stops compilation and reports the C# error.

Diagnostics include:

- Source file path.
- Line number.
- Column number.
- Diagnostic code.
- Error message.
- Severity.

The compiler also provides a diagnostics mode that can be used by developer tooling:

```bash
SvelteSharp --diagnostics Counter.svelte
```

This returns diagnostics in JSON format, making integration with editors and development tools possible.

**Note:** Semantic validation currently covers supported C# members and expressions. It does not yet provide complete C# semantic analysis or full validation of every Svelte markup expression.

## How It Works

SvelteSharp combines a C# compiler, Roslyn semantic analysis, and a Razor-inspired markup translator with the existing Svelte compilation pipeline.

```text
             C# Component
              .svelte.cs
             or @code block
                   |
                   v
          SvelteSharp Compiler
              (.NET / Roslyn)
                   |
                   v
             Syntax Validation
                   |
                   v
            Semantic Validation
                   |
                   v
          C# to JavaScript Translation
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
             Generated Output
                   |
                   v
                 Browser
```

The markup parser recognizes supported Razor-style directives and converts them into an intermediate representation.

The markup translator processes these nodes and generates corresponding Svelte syntax.

This architecture separates parsing, intermediate representation, and code generation, making it easier to expand the supported markup language in future versions.

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
- Semantic validation covers only supported C# constructs.
- Some advanced C# language features are not implemented.
- Advanced Blazor and Razor functionality is not currently supported.
- Razor-style markup support is limited to implemented directives.
- Advanced C# for loop patterns are not supported.
- Full C# switch semantics and all switch patterns are not implemented.
- Advanced Razor features such as templates, sections, and tag helpers are not supported.
- Generated Svelte expressions must follow Svelte's syntax and compatibility rules.

## Roadmap

The following are development goals, not promises of existing functionality.

- [ ] Simplified project initialization.
- [ ] Expanded compiler diagnostics.
- [ ] Complete markup-to-C# member validation.
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