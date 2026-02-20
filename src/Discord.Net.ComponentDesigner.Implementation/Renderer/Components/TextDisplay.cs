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
                ("id", textDisplay.Id, CSharpValueGenerator.NullableInteger),
                ("content", textDisplay.Content, new(RenderTextDisplayContent))
            ),
            (symbol, parameters) => new RenderedComponent(
                $"new {symbol.ToQualifiedName()}({parameters})",
                symbol
            )
        );

    private static Result<string> RenderTextDisplayContent(
        IRendererContext context,
        ComponentPropertyValue value,
        CancellationToken cancellationToken
    )
    {
        if (value is ComponentPropertyValue.AttributeValue attributeValue)
        {
            return CSharpValueGenerator
                .String
                .Render(
                    context,
                    attributeValue,
                    cancellationToken: cancellationToken
                );
        }
        
        if (value.GraphNode is null)
            return Diagnostic
                .InvalidPropertyValue(value, ComponentPropertyValueKind.Component)
                .At(value);

        // should always expect text control
        if (value.GraphNode.Component is not TextControlNode)
            return Diagnostic
                .InvalidPropertyValue(value, "<text control>")
                .At(value);

        // our renderer renders text controls as C# strings, so we just need to call its render function
        return context
            .RenderGraphNode(
                value.GraphNode,
                cancellationToken: cancellationToken
            )
            .Map(x => x.Source);
    }
}