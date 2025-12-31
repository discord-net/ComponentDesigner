namespace Discord.CX.Nodes;

public readonly record struct ComponentRenderingOptions(
    ComponentTypingContext? TypingContext = null
)
{
    public static readonly ComponentRenderingOptions Default = new();
}