using ComponentDesigner;
using ComponentDesigner.Nodes;

namespace Discord;

partial class DiscordNetRenderer
{
    public override Result<RenderedComponent> RenderTextDisplay(
        IRendererContext context,
        TextDisplayComponentNode textDisplay,
        TextDisplayState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    ) => context.CompilationProvider
        .TextDisplayBuilder(state.TextSpan, cancellationToken)
        .Combine(
            RenderPropertiesAsParameters(
                context, state, cancellationToken,
                ("id", textDisplay.Id, CSharpValueGenerator.NullableInt32),
                ("content", textDisplay.Content, new(RenderTextDisplayContent))
            ),
            (symbol, parameters) => new RenderedComponent(
                $"new {symbol.ToQualifiedName()}({parameters})",
                symbol
            )
        )
        .Map(ApplyRefParameter(context, state, cancellationToken))
        .Map(GetConverterFromOptions(context, state, typingContext, cancellationToken));

    private static Result<string> RenderTextDisplayContent(
        IRendererContext context,
        ComponentPropertyValue value,
        CancellationToken cancellationToken
    )
    {
        if (value is ComponentPropertyValue.Component { GraphNode: var graphNode })
        {
            if (graphNode.Component is not TextControlNode)
            {
                return Diagnostic
                    .InvalidPropertyValue(value, "<text control>")
                    .At(value);
            }

            return context
                .RenderGraphNode(
                    graphNode,
                    cancellationToken: cancellationToken
                )
                .AsSource;
        }
        
        return CSharpValueGenerator
            .String
            .Render(
                context,
                value,
                cancellationToken
            );
    }
}