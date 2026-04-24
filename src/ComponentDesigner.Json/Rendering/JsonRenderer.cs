using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using ComponentDesigner.Nodes;
using ComponentDesigner;
using ComponentDesigner.Nodes.TextControls;
using ComponentDesigner.Util;

namespace ComponentDesigner.Json;

public sealed partial class JsonRenderer : IComponentRenderer<JsonArray, JsonNode>
{
    public static readonly JsonRenderer Instance = new();
    
    public Result<JsonArray> RenderGraph(
        IRenderContext<JsonNode> context,
        CXComponentGraph graph,
        CancellationToken cancellationToken = default
    ) => graph.RootNodes
        .Select(x => x.Render(context, cancellationToken))
        .Flatten()
        .Map(x =>
        {
            var arr = new JsonArray();
            arr.AddRange(x);
            return arr;
        });

    public Result<JsonNode> RenderComponent(
        IRenderContext<JsonNode> context,
        IComponentNode component,
        ComponentState state,
        CancellationToken cancellationToken = default
    )
    {
        return (component, state) switch
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

            (FunctionalComponentNode functional, FunctionalState functionalState)
                => RenderFunctionalComponent(context, functional, functionalState, cancellationToken),

            (InterpolationComponentNode interpolation, InterpolationState interpolationState)
                => RenderInterpolation(context, interpolation, interpolationState, cancellationToken),

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

            (TextControlNode, TextControlState textControlState)
                => RenderTextControls(context, textControlState.TextControlGraph, cancellationToken),

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
    }

    public Result<JsonNode> RenderTextControls(
        IRenderContext<JsonNode> context,
        TextControlGraph textControlGraph,
        CancellationToken cancellationToken = default
    )
    {
        return textControlGraph
            .Render(
                context,
                new(InterpolationHandler),
                cancellationToken
            )
            .Map(JsonNode (text) =>
                JsonValue.Create(text.Trim().NormalizeIndentation())
            );

        static Result<string> InterpolationHandler(
            IRenderContext context,
            IInterpolationInfo info,
            out bool valueContainsNewlines
        )
        {
            valueContainsNewlines = false;
            return Diagnostic
                .TypedComponentsAreNotSupported("json")
                .At(info);
        }
    }

    public Result<JsonNode> RenderFunctionalComponent(
        IRenderContext<JsonNode> context,
        FunctionalComponentNode functionalComponent,
        FunctionalState state,
        CancellationToken cancellationToken = default
    ) => Diagnostic.TypedComponentsAreNotSupported("json").At(state.ElementIdentifierTextSpanOrBetter);

    public Result<JsonNode> RenderInterpolation(
        IRenderContext<JsonNode> context,
        InterpolationComponentNode interpolation,
        InterpolationState state,
        CancellationToken cancellationToken = default
    ) => Diagnostic.TypedComponentsAreNotSupported("json").At(state);
}