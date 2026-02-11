using ComponentDesigner;
using ComponentDesigner.Nodes;

namespace Discord.ComponentDesigner;

partial class DiscordNetRenderer
{
    public override Result<RenderedComponent> RenderSection(
        IRendererContext context,
        SectionComponentNode section,
        ComponentState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    ) => context.CompilationProvider
        .SectionBuilder(state.TextSpan, cancellationToken)
        .Combine(
            RenderPropertiesAsParameters(
                context, state, cancellationToken,
                ("id", section.Id, CSharpValueGenerator.NullableInteger),
                ("accessory", section.Accessory, new(RenderAsSingleChildComponent)),
                ("components", section.Components, new(RenderAsChildComponents))
            ),
            (symbol, properties) => new RenderedComponent(
                $"new {symbol.ToQualifiedName()}({properties})",
                symbol
            )
        );
}