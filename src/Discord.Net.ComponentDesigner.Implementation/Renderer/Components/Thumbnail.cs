using ComponentDesigner;
using ComponentDesigner.CSharp;
using ComponentDesigner.Nodes;

namespace Discord;

partial class DiscordNetRenderer
{
    public static Result<CSharpRender> RenderThumbnail(
        IRenderContext<CSharpRender> context,
        ThumbnailComponentNode thumbnail,
        ComponentState state,
        CancellationToken cancellationToken
    ) => Construct(
        context,
        state,
        context.CompilationProvider.ThumbnailBuilder,
        cancellationToken,
        ("id", thumbnail.Id, CSharpValueGenerator.NullableInt32),
        ("media", thumbnail.Media, CSharpValueGenerator.UnfurledMediaItemProperties),
        ("description", thumbnail.Description, CSharpValueGenerator.NullableString),
        ("isSpoiler", thumbnail.Spoiler, CSharpValueGenerator.NullableBoolean)
    );
}