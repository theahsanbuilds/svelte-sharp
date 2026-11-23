
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace SvelteSharp.Compiler;

public static class SvelteSharpSemanticAnalyzer
{
    public static IReadOnlyList<Diagnostic> Analyze(
        IEnumerable<SyntaxTree> syntaxTrees)
    {
        var trees = syntaxTrees.ToArray();

        if (trees.Length == 0)
            return Array.Empty<Diagnostic>();

        // Load .NET platform references safely.
        var trustedAssemblies =
            AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;

        if (string.IsNullOrWhiteSpace(trustedAssemblies))
        {
            throw new InvalidOperationException(
                "Could not load .NET platform reference assemblies.");
        }

        var references = trustedAssemblies
            .Split(
                Path.PathSeparator,
                StringSplitOptions.RemoveEmptyEntries)
            .Where(File.Exists)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(path =>
                MetadataReference.CreateFromFile(path))
            .ToArray();

        // Create Roslyn compilation.
        var compilation = CSharpCompilation.Create(
            "SvelteSharpDiagnostics",
            trees,
            references,
            new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary));

        var supportedSpans =
            new Dictionary<SyntaxTree, List<TextSpan>>();

        // Collect only the C# areas SvelteSharp translates.
        foreach (var tree in trees)
        {
            var spans = new List<TextSpan>();
            var root = tree.GetRoot();

            foreach (var member in root.DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .SelectMany(cls => cls.Members))
            {
                switch (member)
                {
                    case FieldDeclarationSyntax field:

                        spans.Add(field.Declaration.Type.Span);

                        foreach (var variable in field.Declaration.Variables)
                        {
                            if (variable.Initializer is not null)
                                spans.Add(variable.Initializer.Value.Span);
                        }

                        break;

                    case PropertyDeclarationSyntax property:

                        spans.Add(property.Type.Span);

                        if (property.Initializer is not null)
                            spans.Add(property.Initializer.Value.Span);

                        if (property.ExpressionBody is not null)
                            spans.Add(property.ExpressionBody.Expression.Span);

                        var getter = property.AccessorList?
                            .Accessors
                            .FirstOrDefault(a =>
                                a.IsKind(SyntaxKind.GetAccessorDeclaration));

                        if (getter?.Body is not null)
                            spans.Add(getter.Body.Span);

                        break;

                    case MethodDeclarationSyntax method:

                        spans.Add(method.ReturnType.Span);

                        foreach (var parameter in method.ParameterList.Parameters)
                        {
                            if (parameter.Type is not null)
                                spans.Add(parameter.Type.Span);

                            if (parameter.Default is not null)
                                spans.Add(parameter.Default.Value.Span);
                        }

                        if (method.Body is not null)
                            spans.Add(method.Body.Span);

                        if (method.ExpressionBody is not null)
                            spans.Add(method.ExpressionBody.Expression.Span);

                        break;
                }
            }

            supportedSpans[tree] = spans;
        }

        var results = new List<Diagnostic>();

        // Run semantic analysis.
        foreach (var tree in trees)
        {
            var semanticModel = compilation.GetSemanticModel(tree);

            foreach (var diagnostic in semanticModel.GetDiagnostics())
            {
                if (diagnostic.Severity != DiagnosticSeverity.Error ||
                    !diagnostic.Location.IsInSource)
                {
                    continue;
                }

                var diagnosticTree = diagnostic.Location.SourceTree;

                if (diagnosticTree is null ||
                    !supportedSpans.TryGetValue(
                        diagnosticTree, out var spans))
                {
                    continue;
                }

                var diagnosticSpan =
                    diagnostic.Location.SourceSpan;

                if (!spans.Any(span =>
                    span.Contains(diagnosticSpan) ||
                    span.IntersectsWith(diagnosticSpan)))
                {
                    continue;
                }

                // Syntax errors are already reported by Translator.
                var isSyntaxError = diagnosticTree.GetDiagnostics().Any(
                    existing =>
                        existing.Id == diagnostic.Id &&
                        existing.Location.IsInSource &&
                        existing.Location.SourceSpan == diagnosticSpan);

                if (isSyntaxError)
                    continue;

                results.Add(diagnostic);
            }
        }

        return results;
    }
}
