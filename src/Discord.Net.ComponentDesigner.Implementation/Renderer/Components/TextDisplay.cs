using ComponentDesigner;
using ComponentDesigner.CSharp;
using ComponentDesigner.Nodes;

namespace Discord;

partial class DiscordNetRenderer
{
    public static Result<CSharpRender> RenderTextDisplay(
        IRenderContext<CSharpRender> context,
        TextDisplayComponentNode textDisplay,
        TextDisplayState state,
        CancellationToken cancellationToken
    ) => Construct(
        context,
        state,
        context.CompilationProvider.TextDisplayBuilder,
        cancellationToken,
        ("id", textDisplay.Id, CSharpValueGenerator.NullableInt32),
        ("content", textDisplay.Content, TextDisplayContent)
    );

    private static Result<CSharpRender> TextDisplayContent(
        IRenderContext<CSharpRender> context,
        ComponentPropertyValue propertyValue,
        CancellationToken cancellationToken
    ) => propertyValue.AsSingle switch
    {
        ComponentPropertyValue.Component component
            => component.GraphNode.Render(context, cancellationToken),
        _ => CSharpValueGenerator.String.Render(context, propertyValue, cancellationToken)
    };
}