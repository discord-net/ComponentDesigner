using Discord.CX.Nodes.Components;

namespace Discord.CX.Nodes;

public readonly record struct ComponentRenderingOptions(
    ComponentTypingContext? TypingContext = null
)
{
    public static readonly ComponentRenderingOptions Default = new();
}

public readonly record struct ComponentTypingContext(
    bool CanSplat,
    ComponentBuilderKind ConformingType
)
{
    public static readonly ComponentTypingContext Default = new(
        CanSplat: true,
        ConformingType: ComponentBuilderKind.CollectionOfIMessageComponentBuilders
    );
}