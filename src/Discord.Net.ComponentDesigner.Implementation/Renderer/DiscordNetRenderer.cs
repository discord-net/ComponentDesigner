using System.Text;
using ComponentDesigner;
using ComponentDesigner.CSharp;
using ComponentDesigner.Nodes;

namespace Discord;

public sealed partial class DiscordNetRenderer : BaseCSharpRenderer<IReadOnlyList<CSharpRender>>
{
    public static readonly DiscordNetRenderer Instance = new();
    
    public override Result<CSharpRender> RenderComponent(
        IRenderContext<CSharpRender> context,
        IComponentNode component,
        ComponentState state,
        CancellationToken cancellationToken = default
    ) => (component, state) switch
    {
        (ButtonComponentNode button, ButtonState buttonState)
            => RenderButton(context, button, buttonState, cancellationToken),

        (CheckboxComponentNode checkbox, _)
            => RenderCheckbox(context, checkbox, state, cancellationToken),

        (CheckboxGroupComponentNode checkboxGroup, _)
            => RenderCheckboxGroup(context, checkboxGroup, state, cancellationToken),

        (ActionRowComponentNode actionRow, _)
            => RenderActionRow(context, actionRow, state, cancellationToken),

        (ContainerComponentNode container, _)
            => RenderContainer(context, container, state, cancellationToken),

        (FileComponentNode file, _)
            => RenderFile(context, file, state, cancellationToken),

        (FileUploadComponentNode fileUpload, _)
            => RenderFileUpload(context, fileUpload, state, cancellationToken),

        (LabelComponentNode label, _)
            => RenderLabel(context, label, state, cancellationToken),

        (MediaGalleryComponentNode mediaGallery, _)
            => RenderMediaGallery(context, mediaGallery, state, cancellationToken),

        (MediaGalleryItemComponentNode mediaGalleryItem, _)
            => RenderMediaGalleryItem(context, mediaGalleryItem, state, cancellationToken),

        (RadioGroupComponentNode radioGroup, _)
            => RenderRadioGroup(context, radioGroup, state, cancellationToken),

        (RadioGroupOptionComponentNode radioGroupOption, _)
            => RenderRadioGroupOption(context, radioGroupOption, state, cancellationToken),

        (SectionComponentNode section, _)
            => RenderSection(context, section, state, cancellationToken),

        (SelectMenuComponentNode selectMenu, SelectMenuState selectMenuState)
            => RenderSelectMenu(context, selectMenu, selectMenuState, cancellationToken),

        (SelectMenuDefaultValueComponentNode selectMenuDefaultValue, DefaultValueState defaultValueState)
            => RenderSelectMenuDefaultValue(context, selectMenuDefaultValue, defaultValueState, cancellationToken),

        (SelectMenuOptionComponentNode selectMenuOption, _)
            => RenderSelectMenuOption(context, selectMenuOption, state, cancellationToken),

        (SeparatorComponentNode separator, _)
            => RenderSeparator(context, separator, state, cancellationToken),

        (TextDisplayComponentNode textDisplay, TextDisplayState textDisplayState)
            => RenderTextDisplay(context, textDisplay, textDisplayState, cancellationToken),

        (TextInputComponentNode textInput, _)
            => RenderTextInput(context, textInput, state, cancellationToken),

        (ThumbnailComponentNode thumbnail, _)
            => RenderThumbnail(context, thumbnail, state, cancellationToken),

        _ => Diagnostic
            .UnimplementedRendererForComponent("json", component)
            .At(state)
    };

    public override Result<IReadOnlyList<CSharpRender>> RenderGraph(
        IRenderContext<CSharpRender> context,
        CXComponentGraph graph,
        CancellationToken cancellationToken = default
    ) => graph
        .RootNodes
        .Select(node => node.Render(context, cancellationToken))
        .Flatten();
}