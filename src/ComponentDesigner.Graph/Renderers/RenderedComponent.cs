namespace ComponentDesigner;

public record RenderedComponent
{
    public virtual string Source { get; init; }
    public ICSharpTypeSymbol? Type { get; init; }

    public RenderedComponent()
    {
        Source = string.Empty;
        Type = null;
    }

    public RenderedComponent(
        string source,
        ICSharpTypeSymbol? type = null
    )
    {
        Source = source;
        Type = type;
    }

    protected RenderedComponent(ICSharpTypeSymbol? type)
    {
        Source = string.Empty;
        Type = type;
    }

    public static implicit operator RenderedComponent(string source) => new(source);
}