using ComponentDesigner;
using ComponentDesigner.CSharp;
using ComponentDesigner.Nodes;

namespace Discord;

partial class DiscordNetRenderer
{
    public static Result<CSharpRender> RenderContainer(
        IRenderContext<CSharpRender> context,
        ContainerComponentNode container,
        ComponentState state,
        CancellationToken cancellationToken
    ) => Construct(
        context,
        state,
        context.CompilationProvider.ContainerBuilder,
        cancellationToken,
        ("id", container.Id, CSharpValueGenerator.NullableInt32),
        ("accentColor", container.AccentColor, CSharpValueGenerator.Color),
        ("isSpoiler", container.Spoiler, CSharpValueGenerator.NullableBoolean),
        ("components", container.Components, CollectionOfIMessageComponentBuilders)
    );
}