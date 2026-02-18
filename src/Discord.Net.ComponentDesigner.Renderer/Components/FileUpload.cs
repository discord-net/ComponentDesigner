using ComponentDesigner;
using ComponentDesigner.Nodes;

namespace Discord;

partial class DiscordNetRenderer
{
    public override Result<RenderedComponent> RenderFileUpload(
        IRendererContext context,
        FileUploadComponentNode fileUpload,
        ComponentState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    ) => context.CompilationProvider
        .FileUploadComponentBuilder(state.TextSpan, cancellationToken)
        .Combine(
            RenderPropertiesAsParameters(
                context, state, cancellationToken,
                ("id", fileUpload.Id, CSharpValueGenerator.NullableInteger),
                ("customId", fileUpload.CustomId, CSharpValueGenerator.String),
                ("minValues", fileUpload.MinValues, CSharpValueGenerator.NullableInteger),
                ("maxValues", fileUpload.MaxValues, CSharpValueGenerator.NullableInteger),
                ("required", fileUpload.Required, CSharpValueGenerator.Boolean)
            ),
            (symbol, parameters) => new RenderedComponent(
                $"new {symbol.ToQualifiedName()}({parameters})",
                symbol
            )
        );
}