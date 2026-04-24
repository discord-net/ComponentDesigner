using ComponentDesigner;
using ComponentDesigner.CSharp;
using ComponentDesigner.Nodes;

namespace Discord;

partial class DiscordNetRenderer
{
    public static Result<CSharpRender> RenderFile(
        IRenderContext<CSharpRender> context,
        FileComponentNode file,
        ComponentState state,
        CancellationToken cancellationToken
    ) => Construct(
        context,
        state,
        context.CompilationProvider.FileBuilder,
        cancellationToken,
        ("id", file.Id, CSharpValueGenerator.NullableInt32),
        ("media", file.File, CSharpValueGenerator.UnfurledMediaItemProperties),
        ("isSpoiler", file.Spoiler, CSharpValueGenerator.NullableBoolean)
    );
}