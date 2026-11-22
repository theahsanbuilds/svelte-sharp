
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

        if (!File.Exists(codeFile))
            throw new FileNotFoundException(
                $"C# component file not found: {codeFile}",
                codeFile);

        var csharpSource =
            await File.ReadAllTextAsync(codeFile);

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
        string csharpSource,
        string componentName)
    {
        var typescript = TranslateCSharpFile(
            csharpSource,
            componentName);

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

            // Convert an existing script block to TypeScript.
            var scriptTag = match.Value;

            if (Regex.IsMatch(
                scriptTag,
                @"\blang\s*=",
                RegexOptions.IgnoreCase))
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

            var before = svelteSource[..match.Index];

            var after = svelteSource[
                (match.Index + match.Length)..];

            return before +
                   scriptTag +
                   "\n" +
                   typescript +
                   "\n" +
                   after;
        }

        return
            "<script lang=\"ts\">\n" +
            typescript +
            "</script>\n\n" +
            svelteSource;
    }


    // =========================================================
    // PARSE C# COMPONENT
    // =========================================================

    private static string TranslateCSharpFile(
        string source,
        string componentName)
    {
        var tree = CSharpSyntaxTree.ParseText(source);

        var diagnostics = tree.GetDiagnostics()
            .Where(d =>
                d.Severity == DiagnosticSeverity.Error)
            .ToList();

        if (diagnostics.Count > 0)
        {
            var errors = string.Join(
                Environment.NewLine,
                diagnostics.Select(d => d.ToString()));

            throw new InvalidOperationException(
                $"C# syntax errors in {componentName}.svelte.cs:" +
                Environment.NewLine + errors);
        }

        var root = tree.GetRoot();

        var cls = root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .FirstOrDefault();

        if (cls is null)
            throw new InvalidOperationException(
                $"Could not find a class in {componentName}.svelte.cs.");

        if (!cls.Identifier.Text.Equals(
            componentName,
            StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Expected class '{componentName}' but found " +
                $"'{cls.Identifier.Text}'.");
        }

        return TranslateMembers(cls.Members);
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
