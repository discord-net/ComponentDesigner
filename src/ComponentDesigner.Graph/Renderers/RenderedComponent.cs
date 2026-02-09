namespace ComponentDesigner;

public readonly record struct RenderedComponent(
    string Source,
    ICSharpTypeSymbol? Type = null
)
{
    public static implicit operator RenderedComponent(string source) => new(source);
}