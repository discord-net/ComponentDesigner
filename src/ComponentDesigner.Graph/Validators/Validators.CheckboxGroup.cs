using ComponentDesigner.Nodes;

namespace ComponentDesigner;

partial class Validators
{
    public const int CHECKBOX_GROUP_CUSTOM_ID_MIN_LENGTH = 1;
    public const int CHECKBOX_GROUP_CUSTOM_ID_MAX_LENGTH = 100;

    public const int CHECKBOX_GROUP_MIN_OPTIONS = 1;
    public const int CHECKBOX_GROUP_MAX_OPTIONS = 10;
    
    public const int CHECKBOX_GROUP_OPTION_VALUE_MAX_LENGTH = 100;
    public const int CHECKBOX_GROUP_OPTION_LABEL_MAX_LENGTH = 100;
    public const int CHECKBOX_GROUP_OPTION_DESCRIPTION_MAX_LENGTH = 100;

    public static void ValidateCheckboxGroup(
        IComponentContext context,
        CheckboxGroupComponentNode checkboxGroup,
        ComponentState state,
        IDiagnosticBag bag,
        CancellationToken cancellationToken
    )
    {
        ValidateGenericComponent(context, checkboxGroup, state, bag);

        StringRange(
            context, state.GetPropertyValue(checkboxGroup.CustomId), bag,
            upper: CHECKBOX_GROUP_CUSTOM_ID_MAX_LENGTH,
            lower: CHECKBOX_GROUP_CUSTOM_ID_MIN_LENGTH
        );

        var options = state.GetPropertyValue(checkboxGroup.Options);

        PropertyMatchesComponents(
            checkboxGroup, options, bag,
            static x => x
                is IDynamicComponentNode
                or CheckboxGroupOptionComponentNode
        );
        
        Analysis.NumberOfValues(
            context,
            checkboxGroup,
            options,
            cancellationToken,
            out var numberOfOptions
        );

        ValidateMinMax();

        void ValidateMinMax()
        {
            Analysis.TryCreateRangeFromProperties(
                state.GetPropertyValue(checkboxGroup.MinValues),
                state.GetPropertyValue(checkboxGroup.MaxValues),
                out var minMaxRange
            );

            if (minMaxRange.IsInvalid)
            {
                bag.Add(
                    Diagnostic
                        .OutOfRange(
                            checkboxGroup.MinValues,
                            checkboxGroup.MaxValues,
                            minMaxRange.Lower.Value,
                            minMaxRange.Upper.Value
                        )
                        .At(state.GetPropertyValue(checkboxGroup.MinValues))
                );

                return;
            }

            if (
                minMaxRange.Fits(
                    lower: CHECKBOX_GROUP_MIN_OPTIONS,
                    upper: CHECKBOX_GROUP_MAX_OPTIONS
                ) is false
            )
            {
                bag.Add(
                    Diagnostic
                        .OutOfRange(
                            checkboxGroup.Options,
                            (CHECKBOX_GROUP_MIN_OPTIONS, CHECKBOX_GROUP_MAX_OPTIONS),
                            minMaxRange
                        )
                        .At(options)
                );
            }

            StaticRange constrainedRange = (
                minMaxRange.Lower ?? CHECKBOX_GROUP_MIN_OPTIONS,
                minMaxRange.Upper ?? CHECKBOX_GROUP_MAX_OPTIONS
            );

            if (numberOfOptions.Fits(constrainedRange) is false)
            {
                bag.Add(
                    Diagnostic
                        .OutOfRange(
                            checkboxGroup.Options,
                            constrainedRange,
                            numberOfOptions
                        )
                        .At(options)
                );
            }
        }
    }

    public static void ValidateCheckboxGroupOption(
        IComponentContext context,
        CheckboxGroupOptionComponentNode checkboxGroupOption,
        ComponentState state,
        IDiagnosticBag bag
    )
    {
        ValidateGenericComponent(
            context,
            checkboxGroupOption,
            state,
            bag,
            isParentOfOtherComponents: false
        );

        StringNotEmptyAndRange(
            context,
            state.GetPropertyValue(checkboxGroupOption.Value),
            bag,
            upper: CHECKBOX_GROUP_OPTION_VALUE_MAX_LENGTH
        );

        StringNotEmptyAndRange(
            context,
            state.GetPropertyValue(checkboxGroupOption.Label),
            bag,
            upper: CHECKBOX_GROUP_OPTION_LABEL_MAX_LENGTH
        );
        
        StringNotEmptyAndRange(
            context,
            state.GetPropertyValue(checkboxGroupOption.Description),
            bag,
            upper: CHECKBOX_GROUP_OPTION_DESCRIPTION_MAX_LENGTH
        );
    }
}