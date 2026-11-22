using SvelteSharp.Compiler;

if (args.Length == 0)
{
    Console.Error.WriteLine(
        "Usage: SvelteSharp <component.svelte>"
    );

    return;
}

try
{
    string filePath = Path.GetFullPath(args[0]);

    string output = await Translator.TranslateFileAsync(filePath);

    Console.Write(output);
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    Environment.ExitCode = 1;
}