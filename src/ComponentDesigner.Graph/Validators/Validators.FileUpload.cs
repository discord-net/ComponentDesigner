using ComponentDesigner.Nodes;

namespace ComponentDesigner;

partial class Validators
{
    public const int FILE_UPLOAD_CUSTOM_ID_MIN_LENGTH = 1;
    public const int FILE_UPLOAD_CUSTOM_ID_MAX_LENGTH = 100;
    public const int FILE_UPLOAD_MIN_VALUES_LOWER = 0;
    public const int FILE_UPLOAD_MIN_VALUES_UPPER = 10;
    public const int FILE_UPLOAD_MAX_VALUES_LOWER = 1;
    public const int FILE_UPLOAD_MAX_VALUES_UPPER = 10;

    public static void ValidateFileUpload(
        IComponentContext context,
        FileUploadComponentNode fileUpload,
        ComponentState state,
        IDiagnosticBag bag
    )
    {
        ValidateGenericComponent(context, fileUpload, state, bag, isParentOfOtherComponents: false);

        StringNotEmptyAndRange(
            context,
            state.GetPropertyValue(fileUpload.CustomId),
            bag,
            lower: FILE_UPLOAD_CUSTOM_ID_MIN_LENGTH,
            upper: FILE_UPLOAD_CUSTOM_ID_MAX_LENGTH
        );

        var minValueLowerConstraint = FILE_UPLOAD_MIN_VALUES_LOWER;

        var requiredPropertyValue = state.GetPropertyValue(fileUpload.Required);
        
        // 'min_values must be either omitted or at least 1 if required is omitted or true.'
        if (
            requiredPropertyValue is { IsNone: true, IsSourcedFromAttribute: false } ||
            (Analysis.TryGetBooleanValue(requiredPropertyValue, out var required) && required)
        )
        {
            minValueLowerConstraint = 1;
        }

        var minValues = state.GetPropertyValue(fileUpload.MinValues);
        var maxValues = state.GetPropertyValue(fileUpload.MaxValues);
        
        Analysis.TryCreateRangeFromProperties(
            minValues,
            maxValues,
            out var minMaxRange
        );

        if (minMaxRange.IsInvalid)
        {
            bag.Add(
                Diagnostic
                    .OutOfRange(
                        fileUpload.MinValues,
                        fileUpload.MaxValues,
                        minMaxRange.Lower.Value,
                        minMaxRange.Upper.Value
                    )
                    .At(minValues)
            );
        }


        if (minMaxRange.Lower.HasValue)
        {
            IntRange(
                minValues,
                bag,
                minMaxRange.Lower.Value,
                lower: minValueLowerConstraint,
                upper: FILE_UPLOAD_MIN_VALUES_UPPER
            );
        }
        
        if (minMaxRange.Upper.HasValue)
        {
            IntRange(
                maxValues,
                bag,
                minMaxRange.Upper.Value,
                lower: FILE_UPLOAD_MAX_VALUES_LOWER,
                upper: FILE_UPLOAD_MAX_VALUES_UPPER
            );
        }
    }
}