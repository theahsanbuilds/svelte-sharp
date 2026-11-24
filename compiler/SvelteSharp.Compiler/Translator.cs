
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SvelteSharp.Compiler;

public static class Translator
{

        // =========================================================
    // PUBLIC COMPILER DIAGNOSTICS API
    // =========================================================

public static async Task<IReadOnlyList<CompilerDiagnostic>>
    GetDiagnosticsAsync(string svelteFile)
{
    svelteFile = Path.GetFullPath(svelteFile);

    var results = new List<CompilerDiagnostic>();

    if (!File.Exists(svelteFile))
    {
        results.Add(new CompilerDiagnostic(
            svelteFile,
            "Svelte component not found.",
            "error",
            "SSW000",
            1,
            1));

        return results;
    }

    var svelteSource =
        await File.ReadAllTextAsync(svelteFile);

    var codeFile = svelteFile + ".cs";

    string? externalSource = null;

    if (File.Exists(codeFile))
    {
        externalSource =
            await File.ReadAllTextAsync(codeFile);
    }

    var fileName = Path.GetFileName(svelteFile);

    var componentName = fileName switch
    {
        "+page.svelte" => "Page",
        "+layout.svelte" => "Layout",
        _ => Path.GetFileNameWithoutExtension(svelteFile)
    };

    var members =
        new List<(
            MemberDeclarationSyntax Member,
            string FilePath,
            bool IsInline,
            int InlineStart,
            int PrefixLength
        )>();

    var semanticTrees = new List<SyntaxTree>();

    SyntaxTree? inlineTreeForDiagnostics = null;

    int inlineStartForDiagnostics = 0;
    int inlinePrefixLength = 0;

    bool hasSyntaxErrors = false;

    var svelteText =
        Microsoft.CodeAnalysis.Text.SourceText.From(svelteSource);

    // -----------------------------------------
    // EXTERNAL .svelte.cs
    // -----------------------------------------

    if (!string.IsNullOrWhiteSpace(externalSource))
    {
        var tree = CSharpSyntaxTree.ParseText(
            externalSource,
            path: codeFile);

        semanticTrees.Add(tree);

        foreach (var diagnostic in tree.GetDiagnostics()
            .Where(d =>
                d.Severity == DiagnosticSeverity.Error))
        {
            if (!diagnostic.Location.IsInSource)
                continue;

            hasSyntaxErrors = true;

            var position = diagnostic.Location
                .GetLineSpan()
                .StartLinePosition;

            results.Add(new CompilerDiagnostic(
                codeFile,
                diagnostic.GetMessage(),
                "error",
                diagnostic.Id,
                position.Line + 1,
                position.Character + 1));
        }

        var root = tree.GetRoot();

        var cls = root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .FirstOrDefault();

        if (cls is null)
        {
            results.Add(new CompilerDiagnostic(
                codeFile,
                "Could not find a component class.",
                "error",
                "SSW001",
                1,
                1));
        }
        else
        {
            if (!cls.Identifier.Text.Equals(
                componentName,
                StringComparison.Ordinal))
            {
                var position = cls.Identifier
                    .GetLocation()
                    .GetLineSpan()
                    .StartLinePosition;

                results.Add(new CompilerDiagnostic(
                    codeFile,
                    $"Expected class '{componentName}' but found '{cls.Identifier.Text}'.",
                    "error",
                    "SSW002",
                    position.Line + 1,
                    position.Character + 1));
            }

            foreach (var member in cls.Members)
            {
                members.Add((
                    member,
                    codeFile,
                    false,
                    0,
                    0));
            }
        }
    }

    // -----------------------------------------
    // INLINE @code
    // -----------------------------------------

    var inline = ExtractInlineCode(svelteSource);

    if (inline is not null)
    {
       var inlineStart = inline.CodeStart;

        var prefix =
            $"partial class {componentName}\n" +
            "{\n";

        var wrappedSource =
            prefix +
            inline.Code +
            "\n}";

        var tree = CSharpSyntaxTree.ParseText(
            wrappedSource,
            path: svelteFile);

        inlineTreeForDiagnostics = tree;
        inlineStartForDiagnostics = inlineStart;
        inlinePrefixLength = prefix.Length;

        semanticTrees.Add(tree);

        foreach (var diagnostic in tree.GetDiagnostics()
            .Where(d =>
                d.Severity == DiagnosticSeverity.Error))
        {
            if (!diagnostic.Location.IsInSource)
                continue;

            hasSyntaxErrors = true;

            var originalPosition =
                inlineStart +
                diagnostic.Location.SourceSpan.Start -
                prefix.Length;

            originalPosition = Math.Clamp(
                originalPosition,
                0,
                svelteSource.Length);

            var position = svelteText.Lines
                .GetLinePosition(originalPosition);

            results.Add(new CompilerDiagnostic(
                svelteFile,
                diagnostic.GetMessage(),
                "error",
                diagnostic.Id,
                position.Line + 1,
                position.Character + 1));
        }

        var root = tree.GetRoot();

        var cls = root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .FirstOrDefault();

        if (cls is not null)
        {
            foreach (var member in cls.Members)
            {
                members.Add((
                    member,
                    svelteFile,
                    true,
                    inlineStart,
                    prefix.Length));
            }
        }
    }

    // -----------------------------------------
    // DUPLICATE MEMBER VALIDATION
    // -----------------------------------------

    var declaredNames =
        new Dictionary<string, string>(
            StringComparer.Ordinal);

    foreach (var item in members)
    {
        foreach (var name in GetMemberNames(item.Member))
        {
            if (declaredNames.ContainsKey(name))
            {
                var sourceLocation =
                    item.Member.GetLocation();

                int line;
                int column;

                if (item.IsInline)
                {
                    var originalPosition =
                        item.InlineStart +
                        sourceLocation.SourceSpan.Start -
                        item.PrefixLength;

                    originalPosition = Math.Clamp(
                        originalPosition,
                        0,
                        svelteSource.Length);

                    var position = svelteText.Lines
                        .GetLinePosition(originalPosition);

                    line = position.Line + 1;
                    column = position.Character + 1;
                }
                else
                {
                    var position = sourceLocation
                        .GetLineSpan()
                        .StartLinePosition;

                    line = position.Line + 1;
                    column = position.Character + 1;
                }

                results.Add(new CompilerDiagnostic(
                    item.FilePath,
                    $"Duplicate member '{name}'.",
                    "error",
                    "SSW003",
                    line,
                    column));
            }
            else
            {
                declaredNames.Add(
                    name,
                    item.FilePath);
            }
        }
    }

    // -----------------------------------------
    // SEMANTIC VALIDATION
    // -----------------------------------------

    // Do not run semantic analysis on malformed syntax.
    // This avoids misleading follow-up diagnostics.

    if (!hasSyntaxErrors && semanticTrees.Count > 0)
    {
        var semanticDiagnostics =
            SvelteSharpSemanticAnalyzer.Analyze(
                semanticTrees);

        foreach (var diagnostic in semanticDiagnostics)
        {
            if (!diagnostic.Location.IsInSource)
                continue;

            var sourceTree =
                diagnostic.Location.SourceTree;

            if (sourceTree is null)
                continue;

            int line;
            int column;
            string diagnosticFile;

            if (sourceTree == inlineTreeForDiagnostics)
            {
                var originalPosition =
                    inlineStartForDiagnostics +
                    diagnostic.Location.SourceSpan.Start -
                    inlinePrefixLength;

                originalPosition = Math.Clamp(
                    originalPosition,
                    0,
                    svelteSource.Length);

                var position = svelteText.Lines
                    .GetLinePosition(originalPosition);

                line = position.Line + 1;
                column = position.Character + 1;

                diagnosticFile = svelteFile;
            }
            else
            {
                var position = diagnostic.Location
                    .GetLineSpan()
                    .StartLinePosition;

                line = position.Line + 1;
                column = position.Character + 1;

                diagnosticFile = codeFile;
            }

            results.Add(new CompilerDiagnostic(
                diagnosticFile,
                diagnostic.GetMessage(),
                "error",
                "SSW004",
                line,
                column));
        }
    }

    return results;
}

    // =========================================================
    // OLD MODE
    // =========================================================

    public static string Translate(string source)
    {
        var cleanSource = string.Join(
            "\n",
            source.Split('\n')
                .Where(line =>
                    !line.TrimStart().StartsWith("using "))
        );

        var wrapped =
            "class __SvelteSharpScript {\n" +
            cleanSource +
            "\n}";

        var tree = CSharpSyntaxTree.ParseText(wrapped);
        var root = tree.GetRoot();

        var cls = root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .FirstOrDefault();

        if (cls is null)
            throw new InvalidOperationException(
                "Could not parse script body as C# members.");

        return TranslateMembers(cls.Members);
    }


    // =========================================================
    // READ SVELTE AND C# FILES
    // =========================================================
public static async Task<string> TranslateFileAsync(
    string svelteFile)
{
    svelteFile = Path.GetFullPath(svelteFile);

    if (!File.Exists(svelteFile))
        throw new FileNotFoundException(
            "Svelte component not found.",
            svelteFile);

    var svelteSource =
        await File.ReadAllTextAsync(svelteFile);

    var codeFile = svelteFile + ".cs";

    string? csharpSource = null;

    if (File.Exists(codeFile))
    {
        csharpSource =
            await File.ReadAllTextAsync(codeFile);
    }

    var fileName = Path.GetFileName(svelteFile);

    var componentName = fileName switch
    {
        "+page.svelte" => "Page",
        "+layout.svelte" => "Layout",
        _ => Path.GetFileNameWithoutExtension(svelteFile)
    };

    return TranslateComponent(
        svelteSource,
        csharpSource,
        componentName);
}


    // =========================================================
    // COMPONENT TRANSLATION
    // =========================================================

    private static string TranslateComponent(
        string svelteSource,
        string? csharpSource,
        string componentName)
    {
        var inline = ExtractInlineCode(svelteSource);

        var typescript = TranslateCSharpSources(
            csharpSource,
            inline?.Code,
            componentName);

        if (inline is not null)
        {
            svelteSource = inline.Markup;
        }
svelteSource = RazorMarkupTranslator.Transform(svelteSource);
        var match = Regex.Match(
            svelteSource,
            @"<script\b[^>]*>",
            RegexOptions.IgnoreCase);

        if (match.Success)
        {
            if (Regex.IsMatch(
                match.Value,
                @"\blang\s*=\s*[""']csharp[""']",
                RegexOptions.IgnoreCase))
            {
                throw new InvalidOperationException(
                    "Remove the old <script lang=\"csharp\"> block.");
            }

            var scriptTag = match.Value;

            if (Regex.IsMatch(scriptTag, @"\blang\s*="))
            {
                scriptTag = Regex.Replace(
                    scriptTag,
                    @"\blang\s*=\s*[""'][^""']*[""']",
                    "lang=\"ts\"",
                    RegexOptions.IgnoreCase);
            }
            else
            {
                scriptTag = scriptTag.Insert(
                    scriptTag.Length - 1,
                    " lang=\"ts\"");
            }

            return
                svelteSource[..match.Index] +
                scriptTag +
                "\n" +
                typescript +
                "\n" +
                svelteSource[(match.Index + match.Length)..];
        }

        return
            "<script lang=\"ts\">\n" +
            typescript +
            "\n</script>\n\n" +
            svelteSource;
    }


    // =========================================================
    // EXTRACT INLINE @code
    // =========================================================
private sealed record InlineCodeBlock(
    string Code,
    string Markup,
    int CodeStart,
    int DirectiveStart,
    int ClosingBrace
);
    
private static InlineCodeBlock? ExtractInlineCode(string source)
{
    var match = Regex.Match(
        source,
        @"@code\b\s*\{");

    if (!match.Success)
        return null;

    var openBrace = match.Index + match.Length - 1;
    var codeStart = openBrace + 1;

    // Let Roslyn tokenize the source from the opening brace.
    // String contents and comments are handled by the C# lexer.
    var parseOptions = CSharpParseOptions.Default
        .WithLanguageVersion(LanguageVersion.Preview);

    var tokens = SyntaxFactory.ParseTokens(
        source[openBrace..],
        options: parseOptions);

    var depth = 0;
    var closeBrace = -1;

    foreach (var token in tokens)
    {
        if (token.IsKind(SyntaxKind.EndOfFileToken))
            break;

        if (token.IsKind(SyntaxKind.OpenBraceToken))
        {
            depth++;
        }
        else if (token.IsKind(SyntaxKind.CloseBraceToken))
        {
            depth--;

            if (depth == 0)
            {
                closeBrace = openBrace + token.SpanStart;
                break;
            }
        }
    }

    if (closeBrace < 0)
    {
        throw new InvalidOperationException(
            "Unclosed @code block. Check your C# syntax.");
    }

    var remaining = source[(closeBrace + 1)..];

    if (Regex.IsMatch(remaining, @"@code\b\s*\{"))
    {
        throw new InvalidOperationException(
            "Only one @code block is supported per component.");
    }

    var code = source[codeStart..closeBrace];

    var markup =
        source[..match.Index] +
        remaining;

    return new InlineCodeBlock(
        code,
        markup,
        codeStart,
        match.Index,
        closeBrace);
}


    // =========================================================
    // FIND MATCHING C# BRACE
    // =========================================================

    private static int FindMatchingBrace(
        string source,
        int openBrace)
    {
        var depth = 1;
        var i = openBrace + 1;

        while (i < source.Length)
        {
            var current = source[i];

            // Line comment.
            if (current == '/' &&
                i + 1 < source.Length &&
                source[i + 1] == '/')
            {
                i += 2;

                while (i < source.Length &&
                       source[i] != '\n')
                {
                    i++;
                }

                continue;
            }

            // Block comment.
            if (current == '/' &&
                i + 1 < source.Length &&
                source[i + 1] == '*')
            {
                i += 2;

                while (i + 1 < source.Length &&
                       !(source[i] == '*' &&
                         source[i + 1] == '/'))
                {
                    i++;
                }

                i = Math.Min(source.Length, i + 2);
                continue;
            }

            // String.
            if (current == '"')
            {
                i = SkipString(source, i);
                continue;
            }

            // Character.
            if (current == '\'')
            {
                i = SkipCharacter(source, i);
                continue;
            }

            if (current == '{')
                depth++;

            if (current == '}')
            {
                depth--;

                if (depth == 0)
                    return i;
            }

            i++;
        }

        return -1;
    }


    private static int SkipString(
        string source,
        int start)
    {
        // Detect raw strings: """ ... """
        var quoteCount = 1;

        while (
            start + quoteCount < source.Length &&
            source[start + quoteCount] == '"')
        {
            quoteCount++;
        }

        if (quoteCount >= 3)
        {
            var delimiter = new string('"', quoteCount);

            var end = source.IndexOf(
                delimiter,
                start + quoteCount,
                StringComparison.Ordinal);

            return end < 0
                ? source.Length
                : end + quoteCount;
        }

        // Detect verbatim strings: @"..."
        // and $@"..."
        var isVerbatim =
            start > 0 &&
            source[start - 1] == '@';

        if (start > 1 &&
            source[start - 1] == '$' &&
            source[start - 2] == '@')
        {
            isVerbatim = true;
        }

        var i = start + 1;

        while (i < source.Length)
        {
            if (isVerbatim)
            {
                if (source[i] == '"')
                {
                    if (i + 1 < source.Length &&
                        source[i + 1] == '"')
                    {
                        i += 2;
                        continue;
                    }

                    return i + 1;
                }
            }
            else
            {
                if (source[i] == '\\')
                {
                    i += 2;
                    continue;
                }

                if (source[i] == '"')
                    return i + 1;
            }

            i++;
        }

        return source.Length;
    }


    private static int SkipCharacter(
        string source,
        int start)
    {
        var i = start + 1;

        while (i < source.Length)
        {
            if (source[i] == '\\')
            {
                i += 2;
                continue;
            }

            if (source[i] == '\'')
                return i + 1;

            i++;
        }

        return source.Length;
    }


    // =========================================================
    // COMBINE C# SOURCES
    // =========================================================

    private static string TranslateCSharpSources(
        string? externalSource,
        string? inlineSource,
        string componentName)
    {
        var members =
            new List<(
                MemberDeclarationSyntax Member,
                string Source
            )>();

        // -----------------------------------------
        // EXTERNAL .svelte.cs
        // -----------------------------------------

        if (!string.IsNullOrWhiteSpace(externalSource))
        {
            var tree = CSharpSyntaxTree.ParseText(
                externalSource);

            var diagnostics = tree.GetDiagnostics()
                .Where(d =>
                    d.Severity == DiagnosticSeverity.Error)
                .ToList();

            if (diagnostics.Count > 0)
            {
                throw new InvalidOperationException(
                    $"C# syntax errors in {componentName}.svelte.cs:\n" +
                    string.Join(
                        Environment.NewLine,
                        diagnostics));
            }

            var root = tree.GetRoot();

            var cls = root.DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .FirstOrDefault();

            if (cls is null)
            {
                throw new InvalidOperationException(
                    $"Could not find a class in {componentName}.svelte.cs.");
            }

            if (!cls.Identifier.Text.Equals(
                componentName,
                StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Expected class '{componentName}' but found " +
                    $"'{cls.Identifier.Text}'.");
            }

            foreach (var member in cls.Members)
            {
                members.Add((
                    member,
                    $"{componentName}.svelte.cs"));
            }
        }

        // -----------------------------------------
        // INLINE @code
        // -----------------------------------------

        if (!string.IsNullOrWhiteSpace(inlineSource))
        {
            var wrappedSource =
                $"partial class {componentName}\n" +
                "{\n" +
                inlineSource +
                "\n}";

            var tree = CSharpSyntaxTree.ParseText(
                wrappedSource);

            var diagnostics = tree.GetDiagnostics()
                .Where(d =>
                    d.Severity == DiagnosticSeverity.Error)
                .ToList();

            if (diagnostics.Count > 0)
            {
                throw new InvalidOperationException(
                    $"C# syntax errors in {componentName}.svelte @code:\n" +
                    string.Join(
                        Environment.NewLine,
                        diagnostics));
            }

            var root = tree.GetRoot();

            var cls = root.DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .FirstOrDefault();

            if (cls is null)
            {
                throw new InvalidOperationException(
                    "Could not parse the inline @code block.");
            }

            foreach (var member in cls.Members)
            {
                members.Add((
                    member,
                    $"{componentName}.svelte @code"));
            }
        }

        if (members.Count == 0)
        {
            throw new InvalidOperationException(
                $"No C# members found for component '{componentName}'.");
        }

        // -----------------------------------------
        // DUPLICATE VALIDATION
        // -----------------------------------------

        ValidateUniqueMembers(members);

        // -----------------------------------------
        // TRANSLATE TOGETHER
        // -----------------------------------------

        var combinedMembers = SyntaxFactory.List(
            members.Select(x => x.Member));

        return TranslateMembers(combinedMembers);
    }


    // =========================================================
    // DUPLICATE MEMBER VALIDATION
    // =========================================================

    private static void ValidateUniqueMembers(
        List<(
            MemberDeclarationSyntax Member,
            string Source
        )> members)
    {
        var declaredNames = new Dictionary<string, string>(
            StringComparer.Ordinal);

        var errors = new List<string>();

        foreach (var item in members)
        {
            foreach (var name in GetMemberNames(item.Member))
            {
                if (declaredNames.TryGetValue(
                    name,
                    out var previousSource))
                {
                    errors.Add(
                        $"Duplicate member '{name}' found in " +
                        $"{previousSource} and {item.Source}.");
                }
                else
                {
                    declaredNames.Add(name, item.Source);
                }
            }
        }

        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                "Duplicate component members detected:\n" +
                string.Join(Environment.NewLine, errors));
        }
    }


    private static IEnumerable<string> GetMemberNames(
        MemberDeclarationSyntax member)
    {
        switch (member)
        {
            case FieldDeclarationSyntax field:

                foreach (var variable in field.Declaration.Variables)
                    yield return variable.Identifier.Text;

                break;

            case PropertyDeclarationSyntax property:
                yield return property.Identifier.Text;
                break;

            case MethodDeclarationSyntax method:
                yield return method.Identifier.Text;
                break;

            case EventDeclarationSyntax eventDeclaration:
                yield return eventDeclaration.Identifier.Text;
                break;

            case EventFieldDeclarationSyntax eventField:

                foreach (var variable in eventField.Declaration.Variables)
                    yield return variable.Identifier.Text;

                break;

            case BaseTypeDeclarationSyntax type:
                yield return type.Identifier.Text;
                break;

            case DelegateDeclarationSyntax delegateDeclaration:
                yield return delegateDeclaration.Identifier.Text;
                break;

            case ConstructorDeclarationSyntax constructor:
                yield return constructor.Identifier.Text;
                break;

            case DestructorDeclarationSyntax destructor:
                yield return destructor.Identifier.Text;
                break;

            case IndexerDeclarationSyntax:
                yield return "this";
                break;
        }
    }

    // =========================================================
    // C# TYPE TO TYPESCRIPT TYPE
    // =========================================================

    private static string MapType(TypeSyntax type)
    {
        if (type is NullableTypeSyntax nullable)
        {
            return MapType(nullable.ElementType) + " | null";
        }

        if (type is ArrayTypeSyntax array)
        {
            return MapType(array.ElementType) + "[]";
        }

        if (type is QualifiedNameSyntax qualified)
        {
            return MapType(qualified.Right);
        }

        if (type is AliasQualifiedNameSyntax alias)
        {
            return MapType(alias.Name);
        }

        if (type is TupleTypeSyntax tuple)
        {
            return "[" +
                string.Join(
                    ", ",
                    tuple.Elements.Select(e =>
                        MapType(e.Type))) +
                "]";
        }

        if (type is GenericNameSyntax generic)
        {
            var name = generic.Identifier.Text;

            var args = generic.TypeArgumentList.Arguments
                .Select(MapType)
                .ToList();

            var itemType = args.FirstOrDefault() ?? "unknown";

            switch (name)
            {
                case "List":
                case "IList":
                case "ICollection":
                case "IEnumerable":
                case "IReadOnlyList":
                case "IReadOnlyCollection":
                case "HashSet":
                    return itemType + "[]";

                case "Dictionary":
                case "IDictionary":
                case "IReadOnlyDictionary":
                    return $"Record<{args[0]}, {args[1]}>";

                case "Task":
                case "ValueTask":
                    return $"Promise<{itemType}>";

                case "Nullable":
                    return itemType + " | null";

                default:
                    return name + "<" +
                        string.Join(", ", args) +
                        ">";
            }
        }

        var typeName = type.ToString();

        return typeName switch
        {
            "int" => "number",
            "long" => "number",
            "short" => "number",
            "byte" => "number",
            "sbyte" => "number",
            "uint" => "number",
            "ulong" => "number",
            "ushort" => "number",
            "float" => "number",
            "double" => "number",
            "decimal" => "number",

            "string" => "string",
            "char" => "string",

            "bool" => "boolean",
            "object" => "unknown",
            "void" => "void",

            "var" => "unknown",

            "Task" => "Promise<void>",
            "ValueTask" => "Promise<void>",

            "DateTime" => "Date",
            "DateTimeOffset" => "Date",

            "Guid" => "string",

            _ => typeName.Replace("global::", "")
        };
    }


    // =========================================================
    // TRANSLATE CLASS MEMBERS
    // =========================================================

    private static string TranslateMembers(
        SyntaxList<MemberDeclarationSyntax> members)
    {
        var output = new StringBuilder();

        var derived = new List<string>();
        var parameters = new List<string>();
        var parameterTypes = new List<string>();

        var needsOnMountImport = false;

        // Collect component parameters.

        foreach (var prop in members.OfType<PropertyDeclarationSyntax>())
        {
            if (!HasAttribute(prop.AttributeLists, "Parameter"))
                continue;

            var name = prop.Identifier.Text;
            var externalName = ToCamelCase(name);

            var type = MapType(prop.Type);

            var init = prop.Initializer is null
                ? null
                : Expr(prop.Initializer.Value);

            var isBindable =
                HasAttribute(prop.AttributeLists, "Bindable");

            if (isBindable)
            {
                if (init is null)
                    parameters.Add(
                        $"{externalName}: {name} = $bindable()");
                else
                    parameters.Add(
                        $"{externalName}: {name} = $bindable({init})");
            }
            else if (init is not null)
            {
                parameters.Add(
                    $"{externalName}: {name} = {init}");
            }
            else
            {
                parameters.Add(
                    $"{externalName}: {name}");
            }

            var optional = init is not null;

            parameterTypes.Add(
                optional
                    ? $"{externalName}?: {type}"
                    : $"{externalName}: {type}");
        }

        // Validate Bindable.

        foreach (var prop in members.OfType<PropertyDeclarationSyntax>())
        {
            if (HasAttribute(prop.AttributeLists, "Bindable") &&
                !HasAttribute(prop.AttributeLists, "Parameter"))
            {
                throw new InvalidOperationException(
                    $"Property '{prop.Identifier.Text}' uses " +
                    "[Bindable] but is missing [Parameter].");
            }
        }

        // Generate typed Svelte props.

        if (parameters.Count > 0)
        {
            output.AppendLine(
                $"let {{ {string.Join(", ", parameters)} }}: " +
                $"{{ {string.Join("; ", parameterTypes)} }} = $props();");

            output.AppendLine();
        }

        // Translate remaining members.

        foreach (var member in members)
        {
            switch (member)
            {
                case FieldDeclarationSyntax field:
                    EmitField(field, output);
                    break;

                case PropertyDeclarationSyntax prop:

                    if (HasAttribute(
                        prop.AttributeLists,
                        "Parameter"))
                        break;

                    EmitProperty(prop, output, derived);
                    break;

                case MethodDeclarationSyntax method:

                    if (HasAttribute(
                        method.AttributeLists,
                        "OnMount"))
                    {
                        needsOnMountImport = true;
                    }

                    EmitMethod(method, output);
                    break;

                default:
                    throw new NotSupportedException(
                        $"Unsupported C# member: {member.Kind()}");
            }
        }

        if (derived.Count > 0)
        {
            output.AppendLine();

            foreach (var item in derived)
                output.AppendLine(item);
        }

        if (needsOnMountImport)
        {
            output.Insert(
                0,
                "import { onMount } from 'svelte';\n");
        }

        return output.ToString();
    }


    // =========================================================
    // FIELDS
    // =========================================================

    private static void EmitField(
        FieldDeclarationSyntax field,
        StringBuilder output)
    {
        var type = MapType(field.Declaration.Type);

        foreach (var variable in field.Declaration.Variables)
        {
            var name = variable.Identifier.Text;

            var init = variable.Initializer is null
                ? "undefined"
                : Expr(variable.Initializer.Value);

            output.AppendLine(
                $"let {name}: {type} = $state({init});");
        }
    }


    // =========================================================
    // PROPERTIES
    // =========================================================

    private static void EmitProperty(
        PropertyDeclarationSyntax prop,
        StringBuilder output,
        List<string> derived)
    {
        var name = prop.Identifier.Text;
        var type = MapType(prop.Type);

        var isState =
            HasAttribute(prop.AttributeLists, "State");

        // Expression-bodied computed property.

        if (prop.ExpressionBody is not null)
        {
            derived.Add(
                $"let {name}: {type} = $derived(" +
                Expr(prop.ExpressionBody.Expression) +
                ");");

            return;
        }

        // Getter with a body.

        var getter = prop.AccessorList?.Accessors
            .FirstOrDefault(a =>
                a.IsKind(SyntaxKind.GetAccessorDeclaration));

        if (getter?.Body is not null)
        {
            var body = Block(getter.Body);

            derived.Add(
                $"let {name}: {type} = $derived.by(() => {{\n" +
                body +
                "\n});");

            return;
        }

        // Ordinary auto-property.

        var init = prop.Initializer is null
            ? "undefined"
            : Expr(prop.Initializer.Value);

        output.AppendLine(
            $"let {name}: {type} = " +
            (isState
                ? $"$state({init});"
                : $"{init};"));
    }


    // =========================================================
    // METHODS
    // =========================================================

    private static void EmitMethod(
        MethodDeclarationSyntax method,
        StringBuilder output)
    {
        // Effect.

        if (HasAttribute(method.AttributeLists, "Effect"))
        {
            output.AppendLine("$effect(() => {");

            output.Append(Block(method.Body));

            output.AppendLine("});");

            return;
        }

        // OnMount.

        if (HasAttribute(method.AttributeLists, "OnMount"))
        {
            output.AppendLine("onMount(() => {");

            output.Append(Block(method.Body));

            output.AppendLine("});");

            return;
        }

        var isAsync = method.Modifiers.Any(
            SyntaxKind.AsyncKeyword);

        var async = isAsync ? "async " : "";

        var args = string.Join(
            ", ",
            method.ParameterList.Parameters.Select(p =>
                p.Identifier.Text +
                ": " + MapType(p.Type!) +
                (p.Default is null
                    ? ""
                    : " = " + Expr(p.Default.Value))
            ));

        var returnType = MapType(method.ReturnType);

        output.AppendLine(
            $"{async}function {method.Identifier.Text}" +
            $"({args}): {returnType} {{");

        if (method.Body is not null)
        {
            output.Append(Block(method.Body));
        }
        else if (method.ExpressionBody is not null)
        {
            output.AppendLine(
                "return " +
                Expr(method.ExpressionBody.Expression) +
                ";");
        }

        output.AppendLine("}");
    }


    // =========================================================
    // STATEMENT HELPERS
    // =========================================================

private static SyntaxTree MakeComponentPartial(SyntaxTree tree)
{
    var root = tree.GetRoot();

    var cls = root.DescendantNodes()
        .OfType<ClassDeclarationSyntax>()
        .FirstOrDefault();

    if (cls is null ||
        cls.Modifiers.Any(SyntaxKind.PartialKeyword))
    {
        return tree;
    }

    var updatedClass = cls.WithModifiers(
        cls.Modifiers.Add(
            SyntaxFactory.Token(SyntaxKind.PartialKeyword)));

    var updatedRoot = root.ReplaceNode(cls, updatedClass);

    return CSharpSyntaxTree.ParseText(
        updatedRoot.ToFullString(),
        (CSharpParseOptions?)tree.Options,
        tree.FilePath,
        Encoding.UTF8);
}

    private static string StatementBody(
        StatementSyntax statement)
    {
        if (statement is BlockSyntax block)
            return Block(block);

        return Stmt(statement);
    }


    private static string Block(BlockSyntax? block)
    {
        if (block is null)
            return "";

        var result = new StringBuilder();

        foreach (var statement in block.Statements)
            result.AppendLine(Stmt(statement));

        return result.ToString();
    }


    private static string Stmt(StatementSyntax statement)
    {
        switch (statement)
        {
            case ReturnStatementSyntax r:
                return r.Expression is null
                    ? "return;"
                    : "return " + Expr(r.Expression) + ";";

            case ExpressionStatementSyntax e:
                return Expr(e.Expression) + ";";

            case LocalDeclarationStatementSyntax l:
            {
                var type = l.Declaration.Type;

                return "let " +
                    string.Join(
                        ", ",
                        l.Declaration.Variables.Select(v =>
                        {
                            var name = v.Identifier.Text;

                            var annotation =
                                type.ToString() == "var"
                                    ? ""
                                    : ": " + MapType(type);

                            return name + annotation +
                                (v.Initializer is null
                                    ? ""
                                    : " = " + Expr(v.Initializer.Value));
                        })) +
                    ";";
            }

            case IfStatementSyntax i:
            {
                var result =
                    $"if ({Expr(i.Condition)}) {{\n" +
                    StatementBody(i.Statement) +
                    "\n}";

                if (i.Else is not null)
                {
                    result +=
                        " else {\n" +
                        StatementBody(i.Else.Statement) +
                        "\n}";
                }

                return result;
            }

            case BlockSyntax b:
                return "{\n" + Block(b) + "}";

            default:
                throw new NotSupportedException(
                    $"Unsupported C# statement: {statement.Kind()}");
        }
    }


    // =========================================================
    // EXPRESSIONS
    // =========================================================

    private static string Expr(ExpressionSyntax expression)
    {
        if (expression is InterpolatedStringExpressionSyntax interpolated)
        {
            var parts = new StringBuilder("`");

            foreach (var content in interpolated.Contents)
            {
                if (content is InterpolatedStringTextSyntax t)
                {
                    parts.Append(
                        t.TextToken.ValueText
                            .Replace("\\", "\\\\")
                            .Replace("`", "\\`")
                            .Replace("${", "\\${"));
                }
                else if (content is InterpolationSyntax i)
                {
                    parts.Append(
                        "${" + Expr(i.Expression) + "}");
                }
            }

            parts.Append('`');

            return parts.ToString();
        }

        return expression.ToString();
    }


    // =========================================================
    // ATTRIBUTE HELPERS
    // =========================================================

    private static bool HasAttribute(
        SyntaxList<AttributeListSyntax> lists,
        string name)
    {
        return lists
            .SelectMany(l => l.Attributes)
            .Any(a =>
            {
                var attributeName = a.Name.ToString();

                var lastName = attributeName
                    .Split('.')
                    .Last();

                return lastName == name ||
                       lastName == name + "Attribute";
            });
    }


    // =========================================================
    // NAMING
    // =========================================================

    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;

        return char.ToLowerInvariant(name[0]) +
               name.Substring(1);
    }
}
