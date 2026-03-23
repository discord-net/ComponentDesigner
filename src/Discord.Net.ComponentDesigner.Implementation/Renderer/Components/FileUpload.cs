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
                ("id", fileUpload.Id, CSharpValueGenerator.NullableInt32),
                ("customId", fileUpload.CustomId, CSharpValueGenerator.String),
                ("minValues", fileUpload.MinValues, CSharpValueGenerator.NullableInt32),
                ("maxValues", fileUpload.MaxValues, CSharpValueGenerator.NullableInt32),
                ("required", fileUpload.Required, CSharpValueGenerator.Boolean)
            ),
            (symbol, parameters) => new RenderedComponent(
                $"new {symbol.ToQualifiedName()}({parameters})",
                symbol
            )
        )
        .Map(GetConverterFromOptions(context, state, typingContext, cancellationToken));
}