
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SvelteSharp.Compiler;

internal sealed class RazorParser
{
    private readonly string _source;
    private int _position;

    private RazorParser(string source)
    {
        _source = source;
    }

    public static IReadOnlyList<RazorNode> Parse(string source)
    {
        var parser = new RazorParser(source);
        return parser.ParseNodes();
    }

private bool StartsWithAt(int position, string value)
{
    return position + value.Length <= _source.Length &&
        string.Compare(
            _source,
            position,
            value,
            0,
            value.Length,
            StringComparison.OrdinalIgnoreCase) == 0;
}

private bool TrySkipRawElement(string name)
{
    if (_position >= _source.Length ||
        _source[_position] != '<')
        return false;

    var opening = "<" + name;

    if (!StartsWithAt(_position, opening))
        return false;

    var afterName = _position + opening.Length;

    if (afterName < _source.Length &&
        !char.IsWhiteSpace(_source[afterName]) &&
        _source[afterName] != '>')
        return false;

    var openingEnd = _source.IndexOf('>', afterName);

    if (openingEnd < 0)
        throw Error($"Unclosed <{name}> element.");

    var closing = "</" + name;

    var closingStart = _source.IndexOf(
        closing,
        openingEnd + 1,
        StringComparison.OrdinalIgnoreCase);

    if (closingStart < 0)
    {
        _position = _source.Length;
        return true;
    }

    var closingEnd = _source.IndexOf(
        '>',
        closingStart + closing.Length);

    if (closingEnd < 0)
        throw Error($"Unclosed </{name}> element.");

    _position = closingEnd + 1;
    return true;
}

    private IReadOnlyList<RazorNode> ParseNodes()
    {
        var nodes = new List<RazorNode>();
        var rawStart = _position;

        while (_position < _source.Length)
        {
if (IsDirective("@while"))
{
    throw Error(
        "@while is recognized but not supported yet. " +
        "Use @foreach or @for for rendering collections.");
}
    // HTML comments are opaque to the Razor parser.
    if (StartsWithAt(_position, "<!--"))
    {
        var commentEnd = _source.IndexOf(
            "-->",
            _position + 4,
            StringComparison.Ordinal);

        if (commentEnd < 0)
            throw Error("Unclosed HTML comment.");

        _position = commentEnd + 3;
        continue;
    }

    // Do not parse Razor directives inside script/style.
    if (TrySkipRawElement("script") ||
        TrySkipRawElement("style"))
    {
        continue;
    }

           if (StartsWithAt(_position, "@*"))
            {
                FlushRaw(nodes, rawStart, _position);

                var end = _source.IndexOf(
                    "*@",
                    _position + 2,
                    StringComparison.Ordinal);

                if (end < 0)
                    throw Error("Unclosed Razor comment.");

                _position = end + 2;
                rawStart = _position;
                continue;
            }

            if (TryParseIf(out var ifNode))
            {
                FlushRaw(nodes, rawStart, _lastNodeStart);
                nodes.Add(ifNode);
                rawStart = _position;
                continue;
            }

            if (TryParseForeach(out var eachNode))
            {
                FlushRaw(nodes, rawStart, _lastNodeStart);
                nodes.Add(eachNode);
                rawStart = _position;
                continue;
            }

            if (TryParseFor(out var forNode))
            {
                FlushRaw(nodes, rawStart, _lastNodeStart);
                nodes.Add(forNode);
                rawStart = _position;
                continue;
            }

            if (TryParseSwitch(out var switchNode))
            {
                FlushRaw(nodes, rawStart, _lastNodeStart);
                nodes.Add(switchNode);
                rawStart = _position;
                continue;
            }

            _position++;
        }

        FlushRaw(nodes, rawStart, _position);

        return nodes;
    }

    private int _lastNodeStart;



private bool TryParseIf(out RazorNode node)
{
    node = null!;

    if (!IsDirective("@if"))
        return false;

    var start = _position;
    var cursor = _position + 3;

    SkipWhitespace(ref cursor);

    if (!TryReadParenthesized(ref cursor, out var condition))
        return false;

    SkipWhitespace(ref cursor);

    if (!TryReadBlock(ref cursor, out var body))
        return false;

    var branches = new List<RazorIfBranch>
    {
        new(condition, body)
    };

    string? elseContent = null;

    while (true)
    {
        var elseCursor = cursor;
        SkipWhitespace(ref elseCursor);

        var hasAt = MatchesWord(elseCursor, "@else");
        var hasElse = MatchesWord(elseCursor, "else");

        if (!hasAt && !hasElse)
            break;

        elseCursor += hasAt ? 5 : 4;
        SkipWhitespace(ref elseCursor);

        if (MatchesWord(elseCursor, "if"))
        {
            elseCursor += 2;
            SkipWhitespace(ref elseCursor);

            if (!TryReadParenthesized(
                ref elseCursor, out var elseCondition))
                throw  Error("Invalid @else if condition.");

            SkipWhitespace(ref elseCursor);

            if (!TryReadBlock(ref elseCursor, out var elseBody))
                throw  Error("Invalid @else if block.");

            branches.Add(
                new RazorIfBranch(elseCondition, elseBody));

            cursor = elseCursor;
            continue;
        }

        if (!TryReadBlock(ref elseCursor, out elseContent))
            throw  Error("Invalid @else block.");

        cursor = elseCursor;
        break;
    }

    _lastNodeStart = start;
    _position = cursor;

    node = new RazorIfNode(branches, elseContent);
    return true;
}


    private bool TryParseForeach(out RazorNode node)
    {
        node = null!;

        if (!IsDirective("@foreach"))
            return false;

        var start = _position;
        var cursor = _position + 8;

        SkipWhitespace(ref cursor);

        if (!TryReadParenthesized(ref cursor, out var header))
            return false;

        SkipWhitespace(ref cursor);

        if (!TryReadBlock(ref cursor, out var body))
            return false;

        var match = Regex.Match(
            header,
            @"^\s*(?:var|[A-Za-z_][A-Za-z0-9_.<>?, ]*)\s+(?<item>[A-Za-z_]\w*)\s+in\s+(?<collection>.+?)\s*$");

        if (!match.Success)
            throw Error(
                "Unsupported @foreach syntax. " +
                "Expected: foreach (var item in Items).");

        _lastNodeStart = start;
        _position = cursor;

        node = new RazorEachNode(
            match.Groups["item"].Value,
            match.Groups["collection"].Value.Trim(),
            body);

        return true;
    }

    private bool TryParseFor(out RazorNode node)
    {
        node = null!;

        if (!IsDirective("@for"))
            return false;

        var startPosition = _position;
        var cursor = _position + 4;

        SkipWhitespace(ref cursor);

        if (!TryReadParenthesized(ref cursor, out var header))
            return false;

        SkipWhitespace(ref cursor);

        if (!TryReadBlock(ref cursor, out var body))
            return false;

        // Supported forms:
        // for (int i = 0; i < 10; i++)
        // for (var i = 10; i >= 0; i--)
        // for (int i = 0; i < 10; i += 2)

        var match = Regex.Match(
            header,
            @"^\s*(?:int|var|long)\s+(?<variable>[A-Za-z_]\w*)\s*=\s*(?<start>[^;]+)\s*;\s*(?<conditionVariable>[A-Za-z_]\w*)\s*(?<operator><=|<|>=|>)\s*(?<end>[^;]+)\s*;\s*(?<incrementVariable>[A-Za-z_]\w*)\s*(?<increment>\+\+|--|\+=\s*.+?|-=\s*.+?)\s*$");

        if (!match.Success)
        {
            throw Error(
                "Unsupported @for syntax. Use a simple integer " +
                "counter, comparison, and increment.");
        }

        var variable = match.Groups["variable"].Value;

        if (match.Groups["conditionVariable"].Value != variable ||
            match.Groups["incrementVariable"].Value != variable)
        {
            throw Error(
                "The @for loop must use the same variable " +
                "in its initializer, condition, and increment.");
        }

        var increment = match.Groups["increment"].Value.Trim();

        var descending =
            match.Groups["operator"].Value.StartsWith(">") ||
            increment == "--" ||
            increment.StartsWith("-=", StringComparison.Ordinal);

        if (descending &&
            (increment == "++" ||
             increment.StartsWith("+=", StringComparison.Ordinal)))
        {
            throw Error("The @for loop increment conflicts with its direction.");
        }

        if (!descending &&
            (increment == "--" ||
             increment.StartsWith("-=", StringComparison.Ordinal)))
        {
            throw Error("The @for loop increment conflicts with its direction.");
        }

        string step = increment switch
        {
            "++" => "1",
            "--" => "1",
            _ => increment[2..].Trim()
        };

        if (step.Length == 0)
            throw Error("The @for loop step cannot be empty.");

        _lastNodeStart = startPosition;
        _position = cursor;

        node = new RazorForNode(
            variable,
            match.Groups["start"].Value.Trim(),
            match.Groups["end"].Value.Trim(),
            step,
            match.Groups["operator"].Value is "<=" or ">=",
            descending,
            body);

        return true;
    }

    private bool TryParseSwitch(out RazorNode node)
    {
        node = null!;

        if (!IsDirective("@switch"))
            return false;

        var start = _position;
        var cursor = _position + 7;

        SkipWhitespace(ref cursor);

        if (!TryReadParenthesized(ref cursor, out var expression))
            return false;

        SkipWhitespace(ref cursor);

        if (!TryReadBlock(ref cursor, out var body))
            return false;

        var cases = ParseSwitchCases(body);

        if (cases.Count == 0)
            throw Error("A Razor switch must contain at least one case.");

        _lastNodeStart = start;
        _position = cursor;

        node = new RazorSwitchNode(expression, cases);
        return true;
    }

    private static IReadOnlyList<RazorSwitchCase> ParseSwitchCases(
        string body)
    {
        var labels = Regex.Matches(
            body,
            @"(?m)^[ \t]*(?<label>case\s+.+?|default)\s*:");

        var cases = new List<RazorSwitchCase>();

        for (var i = 0; i < labels.Count; i++)
        {
            var current = labels[i];
            var contentStart = current.Index + current.Length;

            var contentEnd = i + 1 < labels.Count
                ? labels[i + 1].Index
                : body.Length;

            var content = body[contentStart..contentEnd].Trim();

            content = Regex.Replace(
                content,
                @"\bbreak\s*;\s*$",
                "",
                RegexOptions.Singleline).Trim();

            var label = current.Groups["label"].Value.Trim();

            if (label == "default")
            {
                cases.Add(new RazorSwitchCase(
                    Array.Empty<string>(),
                    content,
                    true));
            }
            else
            {
                var value = label["case".Length..].Trim();

                if (value.Length == 0)
                    throw new InvalidOperationException(
                        "Empty switch case.");

                cases.Add(new RazorSwitchCase(
                    new[] { value },
                    content,
                    false));
            }
        }

        if (labels.Count > 0 &&
            !string.IsNullOrWhiteSpace(body[..labels[0].Index]))
        {
            throw new InvalidOperationException(
                "Code before the first switch case is not supported.");
        }

        return cases;
    }

    private bool TryReadParenthesized(
        ref int cursor,
        out string content)
    {
        content = "";

        if (cursor >= _source.Length ||
            _source[cursor] != '(')
            return false;

        var end = FindMatching(cursor, '(', ')');

        if (end < 0)
            throw Error("Unclosed parenthesized expression.");

        content = _source[(cursor + 1)..end];
        cursor = end + 1;

        return true;
    }

    private bool TryReadBlock(
        ref int cursor,
        out string content)
    {
        content = "";

        if (cursor >= _source.Length ||
            _source[cursor] != '{')
            return false;

        var end = FindMatching(cursor, '{', '}');

        if (end < 0)
            throw Error("Unclosed Razor block.");

        content = _source[(cursor + 1)..end];
        cursor = end + 1;

        return true;
    }

    private int FindMatching(int start, char open, char close)
    {
        var depth = 0;
        var quote = '\0';
        var escaped = false;
        var lineComment = false;
        var blockComment = false;

        for (var i = start; i < _source.Length; i++)
        {
            var c = _source[i];
            var next = i + 1 < _source.Length
                ? _source[i + 1]
                : '\0';

            if (lineComment)
            {
                if (c == '\n')
                    lineComment = false;

                continue;
            }

            if (blockComment)
            {
                if (c == '*' && next == '/')
                {
                    blockComment = false;
                    i++;
                }

                continue;
            }

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

            if (c == '/' && next == '/')
            {
                lineComment = true;
                i++;
                continue;
            }

            if (c == '/' && next == '*')
            {
                blockComment = true;
                i++;
                continue;
            }

            if (c == '"' || c == '\'')
            {
                quote = c;
                continue;
            }

            if (c == open)
                depth++;

            if (c == close)
            {
                depth--;

                if (depth == 0)
                    return i;
            }
        }

        return -1;
    }

    private bool IsDirective(string directive)
    {
        if (!MatchesWord(_position, directive))
            return false;

        // Do not parse escaped @@if.
        return _position == 0 || _source[_position - 1] != '@';
    }

    private bool MatchesWord(int position, string value)
    {
        if (position + value.Length > _source.Length)
            return false;

        if (string.Compare(
            _source,
            position,
            value,
            0,
            value.Length,
            StringComparison.Ordinal) != 0)
            return false;

        var end = position + value.Length;

        return end >= _source.Length ||
            !char.IsLetterOrDigit(_source[end]) &&
            _source[end] != '_';
    }

    private void SkipWhitespace(ref int position)
    {
        while (position < _source.Length &&
               char.IsWhiteSpace(_source[position]))
            position++;
    }

    private void FlushRaw(
        List<RazorNode> nodes,
        int start,
        int end)
    {
        if (end <= start)
            return;

        nodes.Add(new RazorMarkupNode(
            _source[start..end]));
    }

    private InvalidOperationException Error(string message)
    {
        return new InvalidOperationException(
            $"Razor parser error at position {_position}: {message}");
    }
}
