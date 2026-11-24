
using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SvelteSharp.Compiler;

public static class RazorMarkupTranslator
{
    private static readonly Regex DirectiveAttributeRegex = new(
        @"(?<space>\s)@(?<name>[A-Za-z_][A-Za-z0-9_:-]*)(?<between>\s*=\s*)(?:(?<quote>[""'])(?<quotedValue>.*?)\k<quote>|(?<unquotedValue>\{[^}]*\}|[^\s/>]+))",
        RegexOptions.Singleline);

    private static readonly Regex NormalAttributeRegex = new(
        @"(?<name>[A-Za-z_:][A-Za-z0-9_:.-]*)(?<between>\s*=\s*)(?<quote>[""'])(?<value>.*?)\k<quote>",
        RegexOptions.Singleline);

    public static string Transform(string source)
    {
        if (string.IsNullOrEmpty(source))
            return source;

        var nodes = RazorParser.Parse(source);
        var output = new StringBuilder();

        GenerateNodes(nodes, output);

        return output.ToString();
    }

    private static void GenerateNodes(
        System.Collections.Generic.IReadOnlyList<RazorNode> nodes,
        StringBuilder output)
    {
        foreach (var node in nodes)
        {
            switch (node)
            {
                case RazorMarkupNode markup:
                    output.Append(TransformRawMarkup(markup.Content));
                    break;

                case RazorIfNode conditional:
                    GenerateIf(conditional, output);
                    break;

                case RazorEachNode each:
                    GenerateEach(each, output);
                    break;

                case RazorForNode loop:
                    GenerateFor(loop, output);
                    break;

                case RazorSwitchNode selection:
                    GenerateSwitch(selection, output);
                    break;

                default:
                    throw new NotSupportedException(
                        $"Unknown Razor node: {node.GetType().Name}");
            }
        }
    }

    // =========================================================
    // IF / ELSE
    // =========================================================

    private static void GenerateIf(
        RazorIfNode node,
        StringBuilder output)
    {
        for (var i = 0; i < node.Branches.Count; i++)
        {
            var branch = node.Branches[i];

            output.Append(i == 0
                ? "{#if "
                : "{:else if ");

            output.Append(branch.Condition);
            output.Append('}');

            GenerateNested(branch.Content, output);
        }

        if (node.ElseContent is not null)
        {
            output.Append("{:else}");
            GenerateNested(node.ElseContent, output);
        }

        output.Append("{/if}");
    }

    // =========================================================
    // FOREACH
    // =========================================================

    private static void GenerateEach(
        RazorEachNode node,
        StringBuilder output)
    {
        output.Append("{#each ");
        output.Append(node.Collection);
        output.Append(" as ");
        output.Append(node.Variable);
        output.Append('}');

        GenerateNested(node.Content, output);

        output.Append("{/each}");
    }

    // =========================================================
    // FOR
    // =========================================================

    private static void GenerateFor(
        RazorForNode node,
        StringBuilder output)
    {
        var start = node.Start;
        var end = node.End;
        var step = node.Step;

        var difference = node.Descending
            ? $"(({start}) - ({end}))"
            : $"(({end}) - ({start}))";

        var positiveStep = $"Math.abs({step})";

        var length = node.Inclusive
            ? $"Math.floor({difference} / {positiveStep}) + 1"
            : $"Math.ceil({difference} / {positiveStep})";

        var normalizedLength =
            $"Math.max(0, {length})";

        var value = node.Descending
            ? $"({start}) - (__ssIndex * ({step}))"
            : $"({start}) + (__ssIndex * ({step}))";

        output.Append("{#each Array.from({ length: ");
        output.Append(normalizedLength);
        output.Append(" }, (_, __ssIndex) => ");
        output.Append(value);
        output.Append(") as ");
        output.Append(node.Variable);
        output.Append('}');

        GenerateNested(node.Content, output);

        output.Append("{/each}");
    }

    // =========================================================
    // SWITCH
    // =========================================================

    private static void GenerateSwitch(
        RazorSwitchNode node,
        StringBuilder output)
    {
        var normalCases = node.Cases
            .Where(c => !c.IsDefault)
            .ToList();

        var defaultCase = node.Cases
            .FirstOrDefault(c => c.IsDefault);

        for (var i = 0; i < normalCases.Count; i++)
        {
            var current = normalCases[i];

            var condition = string.Join(
                " || ",
                current.Labels.Select(
                    label => $"({node.Expression}) === ({label})"));

            output.Append(i == 0
                ? "{#if "
                : "{:else if ");

            output.Append(condition);
            output.Append('}');

            GenerateNested(current.Content, output);
        }

        if (defaultCase is not null)
        {
            if (normalCases.Count == 0)
            {
                output.Append("{#if true}");
            }
            else
            {
                output.Append("{:else}");
            }

            GenerateNested(defaultCase.Content, output);
        }

        if (normalCases.Count > 0 || defaultCase is not null)
            output.Append("{/if}");
    }

    // =========================================================
    // NESTED RAZOR MARKUP
    // =========================================================

    private static void GenerateNested(
        string source,
        StringBuilder output)
    {
        var nodes = RazorParser.Parse(source);
        GenerateNodes(nodes, output);
    }

    // =========================================================
    // RAW MARKUP
    // =========================================================

    private static string TransformRawMarkup(string source)
    {
        var output = new StringBuilder();
        var position = 0;

        while (position < source.Length)
        {
            if (StartsWith(source, position, "<!--"))
            {
                var end = source.IndexOf(
                    "-->",
                    position + 4,
                    StringComparison.Ordinal);

                if (end < 0)
                {
                    output.Append(source[position..]);
                    break;
                }

                end += 3;
                output.Append(source[position..end]);
                position = end;
                continue;
            }

            if (TryCopyRawElement(
                source, ref position, output, "script") ||
                TryCopyRawElement(
                source, ref position, output, "style"))
            {
                continue;
            }

            if (source[position] == '<' &&
                position + 1 < source.Length &&
                (char.IsLetter(source[position + 1]) ||
                 source[position + 1] == '/' ||
                 source[position + 1] == '!'))
            {
                var end = FindTagEnd(source, position);

                if (end >= 0)
                {
                    var tag = source[position..(end + 1)];

                    output.Append(TransformTag(tag));
                    position = end + 1;
                    continue;
                }
            }

            var nextTag = source.IndexOf('<', position);

if (nextTag < 0)
    nextTag = source.Length;

if (nextTag == position)
{
    output.Append(source[position]);
    position++;
    continue;
}

output.Append(
    TransformText(source[position..nextTag]));

position = nextTag;
        }

        return output.ToString();
    }

    // =========================================================
    // ATTRIBUTES
    // =========================================================

    private static string TransformTag(string tag)
    {
        var transformed = DirectiveAttributeRegex.Replace(
            tag,
            match =>
            {
                var name = match.Groups["name"].Value;

                var value = match.Groups["quote"].Success
                    ? match.Groups["quotedValue"].Value
                    : match.Groups["unquotedValue"].Value;

                var targetName = MapDirectiveName(name);
                var expression = GetAttributeExpression(value);

                return
                    match.Groups["space"].Value +
                    targetName +
                    match.Groups["between"].Value +
                    "{" + expression + "}";
            });

        transformed = NormalAttributeRegex.Replace(
            transformed,
            match =>
            {
                var value = match.Groups["value"].Value;

                if (!TryGetRazorExpression(
                    value, out var expression))
                    return match.Value;

                return
                    match.Groups["name"].Value +
                    match.Groups["between"].Value +
                    "{" + expression + "}";
            });

        return transformed;
    }

    private static string MapDirectiveName(string name)
    {
        if (name == "bind")
            return "bind:value";

        if (name == "ref")
            return "bind:this";

        if (name.StartsWith(
            "bind:",
            StringComparison.Ordinal))
        {
            return "bind:" +
                name["bind:".Length..].ToLowerInvariant();
        }

        if (name.StartsWith(
            "bind-",
            StringComparison.Ordinal))
        {
            return "bind:" +
                name["bind-".Length..].ToLowerInvariant();
        }

        if (name.StartsWith(
            "on",
            StringComparison.Ordinal))
        {
            return name;
        }

        throw new NotSupportedException(
            $"Unsupported Razor attribute '@{name}'.");
    }

    private static string GetAttributeExpression(string value)
    {
        value = value.Trim();

        if (value.StartsWith("{", StringComparison.Ordinal) &&
            value.EndsWith("}", StringComparison.Ordinal))
        {
            return value[1..^1].Trim();
        }

        if (TryGetRazorExpression(value, out var expression))
            return expression;

        return value;
    }

    // =========================================================
    // TEXT EXPRESSIONS
    // =========================================================

    private static string TransformText(string text)
    {
        var output = new StringBuilder();

        for (var i = 0; i < text.Length;)
        {
            if (text[i] != '@')
            {
                output.Append(text[i++]);
                continue;
            }

            if (i + 1 < text.Length && text[i + 1] == '@')
            {
                output.Append('@');
                i += 2;
                continue;
            }

            if (i + 1 < text.Length && text[i + 1] == '(')
            {
                var end = FindMatching(
                    text, i + 1, '(', ')');

                if (end >= 0)
                {
                    output.Append('{');
                    output.Append(text[(i + 2)..end]);
                    output.Append('}');

                    i = end + 1;
                    continue;
                }
            }

            if (i + 1 < text.Length &&
                IsIdentifierStart(text[i + 1]))
            {
                var end = ReadIdentifierExpression(text, i + 1);

                output.Append('{');
                output.Append(text[(i + 1)..end]);
                output.Append('}');

                i = end;
                continue;
            }

            output.Append('@');
            i++;
        }

        return output.ToString();
    }

    private static int ReadIdentifierExpression(
        string source,
        int position)
    {
        var i = position;

        while (i < source.Length)
        {
            if (IsIdentifierPart(source[i]))
            {
                i++;
                continue;
            }

            if (source[i] == '.' &&
                i + 1 < source.Length &&
                IsIdentifierStart(source[i + 1]))
            {
                i += 2;

                while (i < source.Length &&
                       IsIdentifierPart(source[i]))
                    i++;

                continue;
            }

            break;
        }

        return i;
    }

    private static bool TryGetRazorExpression(
        string value,
        out string expression)
    {
        value = value.Trim();

        if (value.StartsWith("@(", StringComparison.Ordinal) &&
            value.EndsWith(")", StringComparison.Ordinal))
        {
            expression = value[2..^1];
            return true;
        }

        if (value.StartsWith("@", StringComparison.Ordinal) &&
            value.Length > 1 &&
            IsIdentifierStart(value[1]))
        {
            expression = value[1..];
            return true;
        }

        expression = "";
        return false;
    }

    // =========================================================
    // RAW ELEMENTS
    // =========================================================

    private static bool TryCopyRawElement(
        string source,
        ref int position,
        StringBuilder output,
        string name)
    {
        var opening = "<" + name;

        if (!StartsWith(source, position, opening))
            return false;

        var afterName = position + opening.Length;

        if (afterName < source.Length &&
            !char.IsWhiteSpace(source[afterName]) &&
            source[afterName] != '>')
            return false;

        var openingEnd = FindTagEnd(source, position);

        if (openingEnd < 0)
            return false;

        var closing = "</" + name;

        var closingStart = source.IndexOf(
            closing,
            openingEnd + 1,
            StringComparison.OrdinalIgnoreCase);

        if (closingStart < 0)
        {
            output.Append(source[position..]);
            position = source.Length;
            return true;
        }

        var closingEnd = FindTagEnd(source, closingStart);

        if (closingEnd < 0)
            return false;

        output.Append(
            source[position..(closingEnd + 1)]);

        position = closingEnd + 1;
        return true;
    }

    // =========================================================
    // HELPERS
    // =========================================================

    private static int FindTagEnd(
        string source,
        int start)
    {
        var quote = '\0';

        for (var i = start; i < source.Length; i++)
        {
            var c = source[i];

            if (quote != '\0')
            {
                if (c == quote)
                    quote = '\0';

                continue;
            }

            if (c == '"' || c == '\'')
            {
                quote = c;
                continue;
            }

            if (c == '>')
                return i;
        }

        return -1;
    }

    private static int FindMatching(
        string source,
        int start,
        char open,
        char close)
    {
        var depth = 0;
        var quote = '\0';
        var escaped = false;

        for (var i = start; i < source.Length; i++)
        {
            var c = source[i];

            if (quote != '\0')
            {
                if (escaped)
                {
                    escaped = false;
                    continue;
                }

                if (c == '\\')
                {
                    escaped = true;
                    continue;
                }

                if (c == quote)
                    quote = '\0';

                continue;
            }

            if (c == '"' || c == '\'')
            {
                quote = c;
                continue;
            }

        if (c == open)
{
    depth++;
}
else if (c == close)
{
    depth--;

    if (depth == 0)
        return i;
}
        }

        return -1;
    }

    private static bool StartsWith(
        string source,
        int position,
        string value)
    {
        return position + value.Length <= source.Length &&
            string.Compare(
                source,
                position,
                value,
                0,
                value.Length,
                StringComparison.OrdinalIgnoreCase) == 0;
    }

    private static bool IsIdentifierStart(char value)
    {
        return char.IsLetter(value) || value == '_';
    }

    private static bool IsIdentifierPart(char value)
    {
        return char.IsLetterOrDigit(value) || value == '_';
    }
}
