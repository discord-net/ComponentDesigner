using ComponentDesigner.Nodes;

namespace ComponentDesigner;

partial class Validators
{
    public const int RADIO_GROUP_CUSTOM_ID_MIN_LENGTH = 1;
    public const int RADIO_GROUP_CUSTOM_ID_MAX_LENGTH = 100;
    public const int RADIO_GROUP_MIN_OPTIONS = 2;
    public const int RADIO_GROUP_MAX_OPTIONS = 10;

    public const int RADIO_GROUP_OPTION_VALUE_MAX_LENGTH = 100;
    public const int RADIO_GROUP_OPTION_LABEL_MAX_LENGTH = 100;
    public const int RADIO_GROUP_OPTION_DESCRIPTION_MAX_LENGTH = 100;

    public static void ValidateRadioGroup(
        IComponentContext context,
        RadioGroupComponentNode radioGroup,
        ComponentState state,
        IDiagnosticBag bag,
        CancellationToken cancellationToken
    )
    {
        ValidateGenericComponent(context, radioGroup, state, bag);

        StringNotEmptyAndRange(
            context, state.GetPropertyValue(radioGroup.CustomId), bag,
            lower: RADIO_GROUP_CUSTOM_ID_MIN_LENGTH,
            upper: RADIO_GROUP_CUSTOM_ID_MAX_LENGTH
        );

        var options = state.GetPropertyValue(radioGroup.Options);

        Analysis.NumberOfValues(
            context,
            radioGroup,
            options,
            cancellationToken,
            out var optionsRange
        );

        if (optionsRange.Fits(RADIO_GROUP_MIN_OPTIONS, RADIO_GROUP_MAX_OPTIONS) is false)
        {
            bag.Add(
                Diagnostic
                    .OutOfRange(
                        radioGroup.Options,
                        (RADIO_GROUP_MIN_OPTIONS, RADIO_GROUP_MAX_OPTIONS),
                        optionsRange
                    )
                    .At(options)
            );
        }
    }

    public static void ValidateRadioGroupOption(
        IComponentContext context,
        RadioGroupOptionComponentNode radioGroupOption,
        ComponentState state,
        IDiagnosticBag bag
    )
    {
        ValidateGenericComponent(
            context,
            radioGroupOption,
            state,
            bag,
            isParentOfOtherComponents: false
        );

        StringNotEmptyAndRange(
            context,
            state.GetPropertyValue(radioGroupOption.Value),
            bag,
            upper: RADIO_GROUP_OPTION_VALUE_MAX_LENGTH
        );

        StringNotEmptyAndRange(
            context,
            state.GetPropertyValue(radioGroupOption.Label),
            bag,
            upper: RADIO_GROUP_OPTION_LABEL_MAX_LENGTH
        );
        
        StringNotEmptyAndRange(
            context,
            state.GetPropertyValue(radioGroupOption.Description),
            bag,
            upper: RADIO_GROUP_OPTION_DESCRIPTION_MAX_LENGTH
        );
    }
}