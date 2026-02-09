namespace ComponentDesigner.Nodes;

public readonly record struct ComponentOptions(
    RendererTypingContext? TypingContext = null
    )
{
    public static readonly ComponentOptions Default = new();
}