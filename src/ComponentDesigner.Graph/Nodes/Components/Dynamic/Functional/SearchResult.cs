namespace ComponentDesigner.Nodes;

public readonly record struct SearchResult(
    ICSharpSymbol Symbol,
    SearchResultKind Kind
);