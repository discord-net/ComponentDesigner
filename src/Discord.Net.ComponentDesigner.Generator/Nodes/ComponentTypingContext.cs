using Discord.CX.Nodes.Components;

namespace Discord.CX.Nodes;

public readonly record struct ComponentTypingContext(
    bool CanSplat,
    ComponentBuilderKind ConformingType
)
{
    public static readonly ComponentTypingContext Default = new(
        CanSplat: true,
        ConformingType: ComponentBuilderKind.CollectionOfIMessageComponentBuilders
    );

    public static readonly ComponentTypingContext SingleBuilder = new(
        CanSplat: false,
        ConformingType: ComponentBuilderKind.IMessageComponentBuilder
    );
}