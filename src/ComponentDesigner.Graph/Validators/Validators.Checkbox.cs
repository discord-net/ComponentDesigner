using ComponentDesigner.Nodes;

namespace ComponentDesigner;

partial class Validators
{
    public const int CHECKBOX_CUSTOM_ID_MIN_LENGTH = 1;
    public const int CHECKBOX_CUSTOM_ID_MAX_LENGTH = 100;
    
    public static void ValidateCheckbox(
        IComponentContext context,
        CheckboxComponentNode checkbox,
        ComponentState state,
        IDiagnosticBag bag
    )
    {
        ValidateGenericComponent(context, checkbox, state, bag);
        
        StringRange(
            context, state.GetPropertyValue(checkbox.CustomId), bag,
            upper: CHECKBOX_CUSTOM_ID_MAX_LENGTH,
            lower: CHECKBOX_CUSTOM_ID_MIN_LENGTH
        );
    }
}