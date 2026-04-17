using ComponentDesigner.Nodes;

namespace ComponentDesigner;

partial class Validators
{
    public const int TEXT_INPUT_CUSTOM_ID_MIN_LENGTH = 1;
    public const int TEXT_INPUT_CUSTOM_ID_MAX_LENGTH = 100;
    public const int TEXT_INPUT_MIN_LENGTH_LOWER = 0;
    public const int TEXT_INPUT_MIN_LENGTH_UPPER = 4000;
    public const int TEXT_INPUT_MAX_LENGTH_LOWER = 1;
    public const int TEXT_INPUT_MAX_LENGTH_UPPER = 4000;
    public const int TEXT_INPUT_VALUE_MIN_LENGTH = 0;
    public const int TEXT_INPUT_VALUE_MAX_LENGTH = 4000;
    public const int TEXT_INPUT_PLACEHOLDER_MAX_LENGTH = 100;

    public static void ValidateTextInput(
        IComponentContext context,
        TextInputComponentNode textInput,
        ComponentState state,
        IDiagnosticBag bag
    )
    {
        ValidateGenericComponent(textInput, state, bag);

        StringNotEmptyAndRange(
            context, state.GetPropertyValue(textInput.CustomId), bag,
            lower: TEXT_INPUT_CUSTOM_ID_MIN_LENGTH,
            upper: TEXT_INPUT_CUSTOM_ID_MAX_LENGTH
        );

        var minLength = state.GetPropertyValue(textInput.MinLength);
        var maxLength = state.GetPropertyValue(textInput.MaxLength);

        Analysis.TryCreateRangeFromProperties(
            minLength,
            maxLength,
            out var valueRange
        );

        if (valueRange.IsInvalid)
        {
            bag.Add(
                Diagnostic
                    .OutOfRange(
                        textInput.MinLength,
                        textInput.MaxLength,
                        valueRange.Lower.Value,
                        valueRange.Upper.Value
                    )
                    .At(minLength)
            );
        }

        IntRange(
            minLength, bag,
            valueRange.Lower,
            lower: TEXT_INPUT_MIN_LENGTH_LOWER,
            upper: TEXT_INPUT_MIN_LENGTH_UPPER
        );

        IntRange(
            maxLength, bag,
            valueRange.Upper,
            lower: TEXT_INPUT_MAX_LENGTH_LOWER,
            upper: TEXT_INPUT_MAX_LENGTH_UPPER
        );

        var value = state.GetPropertyValue(textInput.Value);

        if (Analysis.TryGetStringValue(value, out var valueStr))
        {
            StaticRange bounds = (
                valueRange.Lower ?? TEXT_INPUT_VALUE_MIN_LENGTH,
                valueRange.Upper ?? TEXT_INPUT_VALUE_MAX_LENGTH
            );

            if (!bounds.Contains(valueStr.Length))
                bag.Add(
                    Diagnostic
                        .OutOfRange(textInput.Value, bounds, valueStr.Length)
                        .At(value)
                );
        }

        StringRange(
            context, state.GetPropertyValue(textInput.Placeholder), bag,
            upper: TEXT_INPUT_PLACEHOLDER_MAX_LENGTH
        );
    }
}