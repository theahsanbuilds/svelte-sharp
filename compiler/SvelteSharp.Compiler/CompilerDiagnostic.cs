namespace SvelteSharp.Compiler;

public sealed record CompilerDiagnostic(
    string FilePath,
    string Message,
    string Severity,
    string Code,
    int Line,
    int Column
);