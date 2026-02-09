using System.Diagnostics;
using ComponentDesigner.Nodes;
using ComponentDesigner.Parser;
using ComponentDesigner.Util;

namespace ComponentDesigner;

public static class ValueValidators
{
    public static void PropertyRange(
        IComponentContext context,
        ComponentState state,
        ComponentProperty lower,
        ComponentProperty upper,
        IDiagnosticBag bag
    )
    {
        var lowerPropertyValue = state.GetPropertyValue(lower);
        var upperPropertyValue = state.GetPropertyValue(upper);

        if (
            lowerPropertyValue is not ComponentPropertyValue.AttributeValue { Attribute.Value: { } lowerValue } ||
            upperPropertyValue is not ComponentPropertyValue.AttributeValue { Attribute.Value: { } upperValue }
        ) return;

        if (
            !TryGetIntValue(context, lowerValue, out var lowerInt) ||
            !TryGetIntValue(context, upperValue, out var upperInt)
        ) return;

        if (lowerInt > upperInt)
        {
            bag.Add(
                upperPropertyValue.TextSpan.Report(
                    Diagnostic.OutOfRange(lower, upper, lowerInt, upperInt)
                )
            );
        }
    }

    public static void IntRange(
        IComponentContext context,
        ComponentState state,
        ComponentProperty property,
        IDiagnosticBag bag,
        int? lower = null,
        int? upper = null
    ) => Range(context, state, property, bag, asString: false, lower, upper);

    public static void StringRange(
        IComponentContext context,
        ComponentState state,
        ComponentProperty property,
        IDiagnosticBag bag,
        int? lower = null,
        int? upper = null
    ) => Range(context, state, property, bag, asString: true, lower, upper);

    public static void Range(
        IComponentContext context,
        ComponentState state,
        ComponentProperty property,
        IDiagnosticBag bag,
        bool asString,
        int? lower = null,
        int? upper = null
    )
    {
        Debug.Assert(lower.HasValue || upper.HasValue);

        var propertyValue = state.GetPropertyValue(property);

        if (propertyValue is not ComponentPropertyValue.AttributeValue { Attribute.Value: { } cxValue }) return;

        int num;

        switch (cxValue)
        {
            case null or CXValue.Invalid: return;

            case CXValue.Interpolation interpolation:
            {
                var constant = context.GetInterpolationInfo(interpolation).ConstantValue;

                if (!constant.IsSpecified) return;

                if (constant.Value is string str && asString) Check(str.Length);
                else if (
                    constant.Value?.GetType().IsNumeric is true &&
                    int.TryParse(constant.Value.ToString(), out num)
                ) Check(num);

                break;
            }

            case CXValue.Multipart { HasInterpolations: false, Tokens: { } tokens } when !asString:
                if (int.TryParse(tokens.ToString(), out num)) Check(num);
                break;

            case CXValue.Multipart literal when asString:
            {
                int? length = null;

                foreach (var token in literal.Tokens)
                {
                    switch (token.Kind)
                    {
                        case CXTokenKind.Text:
                            length ??= 0;
                            length += token.Value.Length;
                            break;
                        case CXTokenKind.Interpolation
                            when token.InterpolationIndex is { } index:
                            var constant = context.GetInterpolationInfo(index).ConstantValue;

                            if (constant.TryGetOfType(out string? strConstant) && !string.IsNullOrEmpty(strConstant))
                            {
                                length ??= 0;
                                length += strConstant.Length;
                            }

                            break;
                    }
                }

                if (length.HasValue) Check(length.Value);

                break;
            }

            case CXValue.Scalar scalar:
            {
                int length;

                if (asString) length = scalar.Value.Length;
                else if (!int.TryParse(scalar.Value, out length))
                    return;

                Check(length);

                return;
            }
        }

        void Check(int target)
        {
            if (target > upper || target < lower)
            {
                bag.Add(
                    propertyValue.TextSpan.Report(
                        asString
                            ? Diagnostic.StringOutOfRange(
                                property,
                                lower,
                                upper,
                                target
                            )
                            : Diagnostic.IntegerOutOfRange(
                                property,
                                lower,
                                upper,
                                target
                            )
                    )
                );
            }
        }
    }

    private static bool TryGetIntValue(IComponentContext context, CXValue? value, out int result)
    {
        switch (value)
        {
            case CXValue.Interpolation interpolation:
                var constant = context.GetInterpolationInfo(interpolation).ConstantValue;

                if (!constant.IsSpecified) break;

                return int.TryParse(constant.Value?.ToString(), out result);

                break;
            case CXValue.Multipart { HasInterpolations: false, Tokens: var tokens }:
                return int.TryParse(tokens.ToString(), out result);

            case CXValue.Scalar scalar:
                return int.TryParse(scalar.Value, out result);
        }

        result = 0;
        return false;
    }
}