# SvelteSharp for Visual Studio Code

**Write C#. Build with Svelte.**

SvelteSharp brings C# component authoring to Svelte 5 and SvelteKit.

This extension provides C# language support and compiler diagnostics for SvelteSharp components.

## Features

- C# component diagnostics.
- Syntax error detection.
- Semantic validation for supported C# constructs.
- Diagnostics in the VS Code Problems panel.
- Support for `.svelte.cs` sidecar files.
- Support for inline `@code` blocks.
- Compiler integration with SvelteSharp.

## Requirements

- Visual Studio Code 1.90.0 or newer.
- SvelteSharp npm package for Svelte project integration.
- Windows x64 for the current compiler distribution.

## Getting Started

1. Install the SvelteSharp extension.
2. Open your SvelteKit project in VS Code.
3. Create a `.svelte` component.
4. Add C# logic using an inline `@code` block or a `.svelte.cs` sidecar file.
5. Open the component to see available compiler diagnostics.

## Example

```csharp
@code {
    private int count = 0;

    private void Increment()
    {
        count++;
    }
}
```

## SvelteSharp Project

- GitHub: https://github.com/theahsanbuilds/svelte-sharp
- npm: https://www.npmjs.com/package/svelte-sharp

## License

MIT