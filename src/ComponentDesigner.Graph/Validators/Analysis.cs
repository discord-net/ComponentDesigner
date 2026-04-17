using System.Diagnostics.CodeAnalysis;
using ComponentDesigner.Nodes;

namespace ComponentDesigner;

internal static class Analysis
{
    public static bool TryGetStringValue(
        ComponentPropertyValue propertyValue,
        [MaybeNullWhen(false)]out string value
    )
    {
        if (propertyValue.IsNone)
        {
            value = null;
            return false;
        }

        switch (propertyValue.AsSingle)
        {
            case ComponentPropertyValue.Literal literal:
                value = literal.Value;
                return true;
            case ComponentPropertyValue.Interpolation{Info.ConstantValue:{IsSpecified: true, Value: {} constant}}:
                value = constant.ToString();
                return true;
        }

        value = null;
        return false;
    }
    
    public static void NumberOfValues(
        IComponentContext context,
        IComponentNode componentNode,
        ComponentPropertyValue propertyValue,
        CancellationToken cancellationToken,
        out StaticRange range
    )
    {
        if (propertyValue.IsNone)
        {
            range = default;
            return;
        }

        if (
            !context.Implementation.TryAnalyzeNumberOfValues(
                context,
                componentNode,
                propertyValue,
                cancellationToken,
                out range
            )
        )
        {
            range = propertyValue.AsFlattened.Count();
        }
    }
    
    public static bool TryGetBooleanValue(
        ComponentPropertyValue propertyValue,
        out bool value
    )
    {
        // only attribute name indicates flag
        if (propertyValue is { IsAttributeNameOnly: true, Property.RequiresValue: false })
        {
            value = true;
            return true;
        }

        switch (propertyValue.AsSingle)
        {
            case ComponentPropertyValue.Literal literal:
                return FromString(literal.Value, out value);
            case ComponentPropertyValue.Interpolation {Info.ConstantValue:{IsSpecified: true, Value: {} constantValue}}:
                if (constantValue is bool v)
                {
                    value = v;
                    return true;
                }

                return FromString(constantValue.ToString(), out value);
        }

        value = false;
        return false;

        static bool FromString(string str, out bool result)
        {
            var lower = str.ToLowerInvariant();
            if (lower is "true" or "false")
            {
                result = lower is "true";
                return true;
            }

            result = false;
            return false;
        }
    }
    
    public static bool TryCreateRangeFromProperties(
        ComponentPropertyValue lower,
        ComponentPropertyValue upper,
        out StaticRange range
    )
    {
        var isValidLower = TryGetInt(lower, out var lowerValue);
        var isValidUpper = TryGetInt(upper, out var upperValue);
        range = new(lowerValue, upperValue);
        return isValidLower && isValidUpper;

        static bool TryGetInt(ComponentPropertyValue propertyValue, out int? result)
        {
            switch (propertyValue.AsSingle)
            {
                case ComponentPropertyValue.Literal { Value: var str }
                    when int.TryParse(str, out var value):
                case ComponentPropertyValue.Interpolation { Info.ConstantValue: { IsSpecified: true } constant }
                    when int.TryParse(constant.ToString(), out value):
                    result = value;
                    return true;
                case ComponentPropertyValue.None when !propertyValue.IsAttributeNameOnly:
                    result = null;
                    return true;
            }

            result = null;
            return false;
        }
    }
}