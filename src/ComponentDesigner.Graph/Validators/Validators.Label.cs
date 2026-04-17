using ComponentDesigner.Nodes;

namespace ComponentDesigner;

partial class Validators
{
    public const int LABEL_LABEL_MAX_LENGTH = 45;
    public const int LABEL_DESCRIPTION_MAX_LENGTH = 100;
    
    public static void ValidateLabel(
        IComponentContext context,
        LabelComponentNode label,
        ComponentState state,
        IDiagnosticBag bag
    )
    {
        ValidateGenericComponent(label, state, bag);

        StringNotEmptyAndRange(
            context, state.GetPropertyValue(label.Label), bag,
            upper: LABEL_LABEL_MAX_LENGTH
        );

        StringRange(
            context, state.GetPropertyValue(label.Description), bag,
            upper: LABEL_DESCRIPTION_MAX_LENGTH
        );

        PropertyMatchesComponents(
            label, state.GetPropertyValue(label.Component), bag,
            static x => x
                is IDynamicComponentNode
                or TextInputComponentNode
                or SelectMenuComponentNode
                or FileUploadComponentNode
                or RadioGroupComponentNode
                or CheckboxGroupComponentNode
                or CheckboxComponentNode
        );
    }
}