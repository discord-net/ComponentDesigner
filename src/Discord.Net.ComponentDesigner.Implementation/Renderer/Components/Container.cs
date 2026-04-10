using ComponentDesigner;
using ComponentDesigner.Nodes;

namespace Discord;

partial class DiscordNetRenderer
{
    public override Result<RenderedComponent> RenderContainer(
        IRendererContext context,
        ContainerComponentNode container,
        ComponentState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    ) => context.CompilationProvider
        .ContainerBuilder(state.TextSpan, cancellationToken)
        .Combine(
            RenderPropertiesAsParameters(
                context, state, cancellationToken,
                ("id", container.Id, CSharpValueGenerator.NullableInt32),
                ("accentColor", container.AccentColor, CSharpValueGenerator.NullableColor),
                ("isSpoiler", container.IsSpoiler, CSharpValueGenerator.NullableBoolean),
                ("components", container.Components, new(RenderContainerComponents))
            ),
            (symbol, parameters) => new RenderedComponent(
                $"new {symbol.ToQualifiedName()}({parameters})"
            )
        )
        .Map(ApplyRefParameter(context, state, cancellationToken))
        .Map(GetConverterFromOptions(context, state, typingContext, cancellationToken));
    
    private static Result<string> RenderContainerComponents(
        IRendererContext context,
        ComponentPropertyValue value,
        CancellationToken cancellationToken
    ) => context
        .CompilationProvider
        .IEnumerableOfIMessageComponentBuilder(value, cancellationToken)
        .Map(symbol => RenderAsChildComponents(context, value, symbol, cancellationToken, true));
}