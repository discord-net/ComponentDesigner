using ComponentDesigner;
using ComponentDesigner.Nodes;

namespace Discord;

partial class DiscordNetRenderer
{
    public override Result<RenderedComponent> RenderLabel(
        IRendererContext context,
        LabelComponentNode label,
        ComponentState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    ) => context.CompilationProvider
        .LabelBuilder(state.TextSpan, cancellationToken)
        .Combine(
            RenderPropertiesAsParameters(
                context, state, cancellationToken,
                ("id", label.Id, CSharpValueGenerator.NullableInt32),
                ("label", label.Value, CSharpValueGenerator.String),
                ("description", label.Description, CSharpValueGenerator.NullableString),
                ("component", label.Component, new(RenderAsSingleChildComponent))
            ),
            (symbol, parameters) => new RenderedComponent(
                $"new {symbol.ToQualifiedName()}({parameters})",
                symbol
            )
        )
        .Map(ApplyRefParameter(context, state, cancellationToken))
        .Map(GetConverterFromOptions(context, state, typingContext, cancellationToken));
}