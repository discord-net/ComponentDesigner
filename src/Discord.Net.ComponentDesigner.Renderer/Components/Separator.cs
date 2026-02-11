using ComponentDesigner;
using ComponentDesigner.Nodes;

namespace Discord.ComponentDesigner;

partial class DiscordNetRenderer
{
    public override Result<RenderedComponent> RenderSeparator(
        IRendererContext context,
        SeparatorComponentNode separator,
        ComponentState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    ) => context.CompilationProvider
        .SeparatorBuilder(state.TextSpan, cancellationToken)
        .Combine(
            CSharpValueGenerator
                .SeparatorSpacingSize(
                    context.CompilationProvider,
                    state.TextSpan,
                    cancellationToken,
                    allowNullable: true
                )
                .Map(spacingSizeGenerator =>
                    RenderPropertiesAsParameters(
                        context, state, cancellationToken,
                        ("id", separator.Id, CSharpValueGenerator.NullableInteger),
                        ("spacing", separator.Spacing, spacingSizeGenerator),
                        ("isDivider", separator.Divider, CSharpValueGenerator.Boolean)
                    )
                ),
            (symbol, parameters) => new RenderedComponent(
                $"new {symbol.ToQualifiedName()}({parameters})",
                symbol
            )
        );
}