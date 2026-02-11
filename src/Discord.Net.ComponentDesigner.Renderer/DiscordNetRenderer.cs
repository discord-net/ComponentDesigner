using ComponentDesigner;
using ComponentDesigner.CSharp;
using ComponentDesigner.Nodes;

namespace Discord.ComponentDesigner;

public sealed partial class DiscordNetRenderer : BaseCSharpRenderer
{
    public override string Name => "Discord.Net";


    public override Result<string> RenderComponents(
        CXComponentGraph graph,
        ComponentEmitContext context,
        CancellationToken cancellationToken = default
    )
    {
        throw new NotImplementedException();
    }
    

   

    public override Result<RenderedComponent> RenderTextInput(IRendererContext context,
        TextInputComponentNode textInput, ComponentState state,
        RendererTypingContext? typingContext = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
    

    public override Result<RenderedComponent> RenderLabel(IRendererContext context, LabelComponentNode label,
        ComponentState state,
        RendererTypingContext? typingContext = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    

    public override Result<RenderedComponent> RenderFileUpload(IRendererContext context,
        FileUploadComponentNode fileUpload, ComponentState state,
        RendererTypingContext? typingContext = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override Result<RenderedComponent> RenderButton(IRendererContext context, ButtonComponentNode button,
        ButtonState state,
        RendererTypingContext? typingContext = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}