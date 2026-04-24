using ComponentDesigner;
using ComponentDesigner.CSharp;
using ComponentDesigner.Nodes;

namespace Discord;

partial class DiscordNetRenderer
{
    public static Result<CSharpRender> RenderActionRow(
        IRenderContext<CSharpRender> context,
        ActionRowComponentNode actionRow,
        ComponentState state,
        CancellationToken cancellationToken = default
    ) => Construct(
        context,
        state,
        context.CompilationProvider.ActionRowBuilder,
        cancellationToken,
        ("id", actionRow.Id, CSharpValueGenerator.NullableInt32),
        ("components", actionRow.Components, CollectionOfIMessageComponentBuilders)
    );
}