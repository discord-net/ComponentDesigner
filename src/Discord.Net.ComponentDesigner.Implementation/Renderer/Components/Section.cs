using ComponentDesigner;
using ComponentDesigner.CSharp;
using ComponentDesigner.Nodes;

namespace Discord;

partial class DiscordNetRenderer
{
    public static Result<CSharpRender> RenderSection(
        IRenderContext<CSharpRender> context,
        SectionComponentNode section,
        ComponentState state,
        CancellationToken cancellationToken
    ) => Construct(
        context,
        state,
        context.CompilationProvider.SectionBuilder,
        cancellationToken,
        ("id", section.Id, CSharpValueGenerator.NullableInt32),
        ("accessory", section.Accessory, IMessageComponentBuilder),
        ("components", section.Components, CollectionOfIMessageComponentBuilders)
    );
}