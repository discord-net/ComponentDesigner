using ComponentDesigner.Nodes;

namespace ComponentDesigner;

public abstract class ComponentVisitor<TResult> : ComponentVisitor<IComponentContext, TResult>;

public abstract class ComponentVisitor<TContext, TResult>
    where TContext : IComponentContext
{
    public virtual TResult Accept(
        TContext context,
        IComponentNode component,
        ComponentState state,
        CancellationToken cancellationToken
    ) => component switch
    {
        ButtonComponentNode button when state is ButtonState buttonState
            => VisitButton(context, button, buttonState, cancellationToken),

        CheckboxComponentNode checkbox
            => VisitCheckbox(context, checkbox, state, cancellationToken),

        CheckboxGroupComponentNode checkboxGroup
            => VisitCheckboxGroup(context, checkboxGroup, state, cancellationToken),

        CheckboxGroupOptionComponentNode checkboxGroupOption
            => VisitCheckboxGroupOption(context, checkboxGroupOption, state, cancellationToken),

        ActionRowComponentNode actionRow
            => VisitActionRow(context, actionRow, state, cancellationToken),

        ContainerComponentNode container
            => VisitContainer(context, container, state, cancellationToken),

        FileComponentNode file
            => VisitFile(context, file, state, cancellationToken),

        FileUploadComponentNode fileUpload
            => VisitFileUpload(context, fileUpload, state, cancellationToken),

        FunctionalComponentNode functional when state is FunctionalState functionalState
            => VisitFunctional(context, functional, functionalState, cancellationToken),

        InterpolationComponentNode interpolation when state is InterpolationState interpolationState
            => VisitInterpolation(context, interpolation, interpolationState, cancellationToken),

        LabelComponentNode label
            => VisitLabel(context, label, state, cancellationToken),

        MediaGalleryComponentNode gallery
            => VisitMediaGallery(context, gallery, state, cancellationToken),

        MediaGalleryItemComponentNode galleryItem
            => VisitMediaGalleryItem(context, galleryItem, state, cancellationToken),

        RadioGroupComponentNode radioGroup
            => VisitRadioGroup(context, radioGroup, state, cancellationToken),

        RadioGroupOptionComponentNode radioGroupOption
            => VisitRadioGroupOption(context, radioGroupOption, state, cancellationToken),

        SectionComponentNode section
            => VisitSection(context, section, state, cancellationToken),

        SelectMenuComponentNode selectMenu when state is SelectMenuState selectMenuState
            => VisitSelectMenu(context, selectMenu, selectMenuState, cancellationToken),

        SelectMenuDefaultValueComponentNode selectMenuDefaultValue when state is DefaultValueState defaultValueState
            => VisitSelectMenuDefaultValue(context, selectMenuDefaultValue, defaultValueState, cancellationToken),

        SelectMenuOptionComponentNode selectMenuOption
            => VisitSelectMenuOption(context, selectMenuOption, state, cancellationToken),

        SeparatorComponentNode separator
            => VisitSeparator(context, separator, state, cancellationToken),

        TextControlNode textControl when state is TextControlState textControlState
            => VisitTextControl(context, textControl, textControlState, cancellationToken),

        TextDisplayComponentNode textDisplay when state is TextDisplayState textDisplayState
            => VisitTextDisplay(context, textDisplay, textDisplayState, cancellationToken),

        TextInputComponentNode textInput
            => VisitTextInput(context, textInput, state, cancellationToken),

        ThumbnailComponentNode thumbnail
            => VisitThumbnail(context, thumbnail, state, cancellationToken),

        _ => VisitGenericComponent(context, component, state, cancellationToken)
    };

    protected abstract TResult VisitGenericComponent(
        TContext context,
        IComponentNode component,
        ComponentState state,
        CancellationToken cancellationToken
    );

    protected virtual TResult VisitButton(
        TContext context,
        ButtonComponentNode component,
        ButtonState state,
        CancellationToken cancellationToken
    ) => VisitGenericComponent(context, component, state, cancellationToken);

    protected virtual TResult VisitCheckbox(
        TContext context,
        CheckboxComponentNode component,
        ComponentState state,
        CancellationToken cancellationToken
    ) => VisitGenericComponent(context, component, state, cancellationToken);

    protected virtual TResult VisitCheckboxGroup(
        TContext context,
        CheckboxGroupComponentNode component,
        ComponentState state,
        CancellationToken cancellationToken
    ) => VisitGenericComponent(context, component, state, cancellationToken);

    protected virtual TResult VisitCheckboxGroupOption(
        TContext context,
        CheckboxGroupOptionComponentNode component,
        ComponentState state,
        CancellationToken cancellationToken
    ) => VisitGenericComponent(context, component, state, cancellationToken);

    protected virtual TResult VisitActionRow(
        TContext context,
        ActionRowComponentNode component,
        ComponentState state,
        CancellationToken cancellationToken
    ) => VisitGenericComponent(context, component, state, cancellationToken);

    protected virtual TResult VisitContainer(
        TContext context,
        ContainerComponentNode component,
        ComponentState state,
        CancellationToken cancellationToken
    ) => VisitGenericComponent(context, component, state, cancellationToken);

    protected virtual TResult VisitFile(
        TContext context,
        FileComponentNode component,
        ComponentState state,
        CancellationToken cancellationToken
    ) => VisitGenericComponent(context, component, state, cancellationToken);

    protected virtual TResult VisitFileUpload(
        TContext context,
        FileUploadComponentNode component,
        ComponentState state,
        CancellationToken cancellationToken
    ) => VisitGenericComponent(context, component, state, cancellationToken);

    protected virtual TResult VisitFunctional(
        TContext context,
        FunctionalComponentNode component,
        FunctionalState state,
        CancellationToken cancellationToken
    ) => VisitGenericComponent(context, component, state, cancellationToken);

    protected virtual TResult VisitInterpolation(
        TContext context,
        InterpolationComponentNode component,
        InterpolationState state,
        CancellationToken cancellationToken
    ) => VisitGenericComponent(context, component, state, cancellationToken);

    protected virtual TResult VisitLabel(
        TContext context,
        LabelComponentNode component,
        ComponentState state,
        CancellationToken cancellationToken
    ) => VisitGenericComponent(context, component, state, cancellationToken);

    protected virtual TResult VisitMediaGallery(
        TContext context,
        MediaGalleryComponentNode component,
        ComponentState state,
        CancellationToken cancellationToken
    ) => VisitGenericComponent(context, component, state, cancellationToken);

    protected virtual TResult VisitMediaGalleryItem(
        TContext context,
        MediaGalleryItemComponentNode component,
        ComponentState state,
        CancellationToken cancellationToken
    ) => VisitGenericComponent(context, component, state, cancellationToken);

    protected virtual TResult VisitRadioGroup(
        TContext context,
        RadioGroupComponentNode component,
        ComponentState state,
        CancellationToken cancellationToken
    ) => VisitGenericComponent(context, component, state, cancellationToken);

    protected virtual TResult VisitRadioGroupOption(
        TContext context,
        RadioGroupOptionComponentNode component,
        ComponentState state,
        CancellationToken cancellationToken
    ) => VisitGenericComponent(context, component, state, cancellationToken);

    protected virtual TResult VisitSection(
        TContext context,
        SectionComponentNode component,
        ComponentState state,
        CancellationToken cancellationToken
    ) => VisitGenericComponent(context, component, state, cancellationToken);

    protected virtual TResult VisitSelectMenu(
        TContext context,
        SelectMenuComponentNode component,
        SelectMenuState state,
        CancellationToken cancellationToken
    ) => VisitGenericComponent(context, component, state, cancellationToken);

    protected virtual TResult VisitSelectMenuDefaultValue(
        TContext context,
        SelectMenuDefaultValueComponentNode component,
        DefaultValueState state,
        CancellationToken cancellationToken
    ) => VisitGenericComponent(context, component, state, cancellationToken);

    protected virtual TResult VisitSelectMenuOption(
        TContext context,
        SelectMenuOptionComponentNode component,
        ComponentState state,
        CancellationToken cancellationToken
    ) => VisitGenericComponent(context, component, state, cancellationToken);

    protected virtual TResult VisitSeparator(
        TContext context,
        SeparatorComponentNode component,
        ComponentState state,
        CancellationToken cancellationToken
    ) => VisitGenericComponent(context, component, state, cancellationToken);

    protected virtual TResult VisitTextControl(
        TContext context,
        TextControlNode component,
        TextControlState state,
        CancellationToken cancellationToken
    ) => VisitGenericComponent(context, component, state, cancellationToken);

    protected virtual TResult VisitTextDisplay(
        TContext context,
        TextDisplayComponentNode component,
        TextDisplayState state,
        CancellationToken cancellationToken
    ) => VisitGenericComponent(context, component, state, cancellationToken);

    protected virtual TResult VisitTextInput(
        TContext context,
        TextInputComponentNode component,
        ComponentState state,
        CancellationToken cancellationToken
    ) => VisitGenericComponent(context, component, state, cancellationToken);

    protected virtual TResult VisitThumbnail(
        TContext context,
        ThumbnailComponentNode component,
        ComponentState state,
        CancellationToken cancellationToken
    ) => VisitGenericComponent(context, component, state, cancellationToken);
}