using ComponentDesigner;
using ComponentDesigner.Nodes;

namespace Discord;

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
                ("id", section.Id, CSharpValueGenerator.NullableInt32),
                ("accessory", section.Accessory, new(RenderAsSingleChildComponent)),
                ("components", section.Components, new(RenderSectionComponents))
            ),
            (symbol, properties) => new RenderedComponent(
                $"new {symbol.ToQualifiedName()}({properties})",
                symbol
            )
        )
        .Map(ApplyRefParameter(context, state, cancellationToken))
        .Map(GetConverterFromOptions(context, state, typingContext, cancellationToken));
    
    private static Result<string> RenderSectionComponents(
        IRendererContext context,
        ComponentPropertyValue value,
        CancellationToken cancellationToken
    ) => context
        .CompilationProvider
        .IEnumerableOfIMessageComponentBuilder(value, cancellationToken)
        .Map(symbol => RenderAsChildComponents(context, value, symbol, cancellationToken, true));
}