using ComponentDesigner;
using ComponentDesigner.CSharp;
using ComponentDesigner.Nodes;

namespace Discord;

partial class DiscordNetRenderer
{
    public static Result<CSharpRender> RenderFileUpload(
        IRenderContext<CSharpRender> context,
        FileUploadComponentNode fileUpload,
        ComponentState state,
        CancellationToken cancellationToken
    ) => Construct(
        context,
        state,
        context.CompilationProvider.FileUploadComponentBuilder,
        cancellationToken,
        ("id", fileUpload.Id, CSharpValueGenerator.NullableInt32),
        ("customId", fileUpload.CustomId, CSharpValueGenerator.String),
        ("minValues", fileUpload.MinValues, CSharpValueGenerator.NullableInt32),
        ("maxValues", fileUpload.MaxValues, CSharpValueGenerator.NullableInt32),
        ("required", fileUpload.Required, CSharpValueGenerator.NullableBoolean)
    );
}