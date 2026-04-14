using System.Threading;
using ComponentDesigner.Nodes;

namespace ComponentDesigner.Json;

partial class JsonRenderer
{
    private const int FILE_UPLOAD_TYPE = 19;

    public Result<RenderedComponent> RenderFileUpload(
        IRendererContext context,
        FileUploadComponentNode fileUpload,
        ComponentState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    ) => Build(
        context,
        state,
        cancellationToken,
        [("type", FILE_UPLOAD_TYPE)],
        ("id", fileUpload.Id, Number),
        ("custom_id", fileUpload.CustomId, String),
        ("min_values", fileUpload.MinValues, Number),
        ("max_values", fileUpload.MaxValues, Number),
        ("required", fileUpload.Required, Bool)
    );
}