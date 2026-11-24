
namespace SvelteSharp.Compiler;

internal abstract record RazorNode;

internal sealed record RazorMarkupNode(string Content) : RazorNode;

internal sealed record RazorIfNode(
    IReadOnlyList<RazorIfBranch> Branches,
    string? ElseContent) : RazorNode;

internal sealed record RazorIfBranch(
    string Condition,
    string Content);

internal sealed record RazorEachNode(
    string Variable,
    string Collection,
    string Content) : RazorNode;

internal sealed record RazorForNode(
    string Variable,
    string Start,
    string End,
    string Step,
    bool Inclusive,
    bool Descending,
    string Content) : RazorNode;

internal sealed record RazorSwitchNode(
    string Expression,
    IReadOnlyList<RazorSwitchCase> Cases) : RazorNode;

internal sealed record RazorSwitchCase(
    IReadOnlyList<string> Labels,
    string Content,
    bool IsDefault);
