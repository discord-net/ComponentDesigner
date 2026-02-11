using ComponentDesigner;
using ComponentDesigner.Nodes;

namespace Discord.ComponentDesigner;

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
                ("id", container.Id, CSharpValueGenerator.NullableInteger),
                ("accentColor", container.AccentColor, CSharpValueGenerator.NullableColor),
                ("isSpoiler", container.IsSpoiler, CSharpValueGenerator.NullableBoolean),
                ("components", container.Components, new(RenderAsChildComponents))
            ),
            (symbol, parameters) => new RenderedComponent(
                $"new {symbol.ToQualifiedName()}({parameters})"
            )
        );
}