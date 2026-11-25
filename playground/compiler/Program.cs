using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices.JavaScript;
using SvelteSharp.Compiler;

namespace SvelteSharp.Playground.Compiler;

public static partial class PlaygroundCompiler
{
    [DynamicDependency(
        DynamicallyAccessedMemberTypes.NonPublicMethods,
        typeof(Translator))]
    [JSExport]
    public static string Translate(
        string svelteSource,
        string csharpSource)
    {
        if (string.IsNullOrWhiteSpace(svelteSource))
            throw new ArgumentException("Svelte source is empty.");

        var method = typeof(Translator).GetMethod(
            "TranslateComponent",
            BindingFlags.NonPublic | BindingFlags.Static);

        if (method is null)
            throw new MissingMethodException(
                "SvelteSharp Translator.TranslateComponent was not found.");

        try
        {
            return (string)method.Invoke(
                null,
                new object[]
                {
                    svelteSource,
                    csharpSource ?? string.Empty,
                    "Playground"
                })!;
        }
        catch (TargetInvocationException ex)
            when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }
    }

    public static void Main()
    {
    }
}