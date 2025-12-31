namespace Discord.CX.Nodes;

public readonly record struct CXValueGeneratorOptions(
    ComponentTypingContext? TypingContext = null
)
{
    public static readonly CXValueGeneratorOptions Default = new();

    public static implicit operator ComponentRenderingOptions(CXValueGeneratorOptions options)
        => new(options.TypingContext);
    
    public static implicit operator CXValueGeneratorOptions(ComponentRenderingOptions options)
        => new(options.TypingContext);
}