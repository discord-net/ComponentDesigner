using ComponentDesigner.Nodes;

namespace ComponentDesigner;

public interface IComponentRenderer
{
    string Name { get; }

    bool IsValidComponentType(
        IComponentContext context, 
        ICSharpTypeSymbol? symbol, 
        CancellationToken cancellationToken = default
    );

    Result<string> RenderComponents(
        CXComponentGraph graph,
        ComponentEmitContext context,
        CancellationToken cancellationToken = default
    );
    
    Result<RenderedComponent> RenderMediaGalleryItem(
        IRendererContext context,
        MediaGalleryItemComponentNode mediaGalleryItem,
        ComponentState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    );
    
    Result<RenderedComponent> RenderMediaGallery(
        IRendererContext context,
        MediaGalleryComponentNode mediaGallery,
        ComponentState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    );
    
    Result<RenderedComponent> RenderSelectMenu(
        IRendererContext context,
        SelectMenuComponentNode selectMenu,
        SelectMenuState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    );
    
    Result<RenderedComponent> RenderSelectMenuOption(
        IRendererContext context,
        SelectMenuOptionComponentNode option,
        ComponentState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    );
    
    Result<RenderedComponent> RenderSelectMenuDefaultValue(
        IRendererContext context,
        SelectMenuDefaultValueComponentNode option,
        DefaultValueState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    );
    
    Result<RenderedComponent> RenderThumbnail(
        IRendererContext context,
        ThumbnailComponentNode thumbnail,
        ComponentState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    );
    
    Result<RenderedComponent> RenderTextInput(
        IRendererContext context,
        TextInputComponentNode textInput,
        ComponentState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    );
    
    Result<RenderedComponent> RenderSeparator(
        IRendererContext context,
        SeparatorComponentNode separator,
        ComponentState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    );
    
    Result<RenderedComponent> RenderSection(
        IRendererContext context,
        SectionComponentNode section,
        ComponentState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    );
    
    Result<RenderedComponent> RenderLabel(
        IRendererContext context,
        LabelComponentNode label,
        ComponentState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    );

    Result<RenderedComponent> RenderFile(
        IRendererContext context,
        FileComponentNode file,
        ComponentState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    );
    
    Result<RenderedComponent> RenderFileUpload(
        IRendererContext context,
        FileUploadComponentNode fileUpload,
        ComponentState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    );

    Result<RenderedComponent> RenderFunctionalComponent(
        IRendererContext context,
        FunctionalComponentNode functionalComponent,
        FunctionalState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    );
    
    Result<RenderedComponent> RenderButton(
        IRendererContext context,
        ButtonComponentNode button,
        ButtonState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    );
    
    Result<RenderedComponent> RenderActionRow(
        IRendererContext context,
        ActionRowComponentNode actionRow,
        ComponentState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    );

    Result<RenderedComponent> RenderInterpolation(
        IRendererContext context,
        IInterpolationInfo info,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    );
    
    Result<RenderedComponent> RenderContainer(
        IRendererContext context,
        ContainerComponentNode container,
        ComponentState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    );

    Result<RenderedComponent> RenderTextDisplay(
        IRendererContext context,
        TextDisplayComponentNode textDisplay,
        TextDisplayState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    );
}