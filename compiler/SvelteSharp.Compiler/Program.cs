
using System.Text.Json;
using SvelteSharp.Compiler;

if (args.Length == 0 ||
    (args[0] == "--diagnostics" && args.Length < 2))
{
    Console.Error.WriteLine(
        "Usage: SvelteSharp [--diagnostics] <component.svelte>"
    );

    Environment.ExitCode = 1;
    return;
}

try
{
    bool diagnosticsMode = args[0] == "--diagnostics";

    string filePath = Path.GetFullPath(
        args[diagnosticsMode ? 1 : 0]
    );

    // Diagnostics mode: return JSON for VS Code.
    if (diagnosticsMode)
    {
        var diagnostics =
            await Translator.GetDiagnosticsAsync(filePath);

        Console.WriteLine(
            JsonSerializer.Serialize(
                new { diagnostics },
                new JsonSerializerOptions
                {
                    WriteIndented = true
                }
            )
        );

        if (diagnostics.Any(d => d.Severity == "error"))
            Environment.ExitCode = 1;

        return;
    }

    // Normal mode: validate before translation.
    var errors = (await Translator.GetDiagnosticsAsync(filePath))
        .Where(d => d.Severity == "error")
        .ToList();

    if (errors.Count > 0)
    {
        foreach (var error in errors)
        {
            Console.Error.WriteLine(
                $"{error.FilePath}({error.Line},{error.Column}): " +
                $"{error.Code}: {error.Message}"
            );
        }

        Environment.ExitCode = 1;
        return;
    }

    // Only translate when there are no errors.
    string output =
        await Translator.TranslateFileAsync(filePath);

    Console.Write(output);
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    Environment.ExitCode = 1;
}
