using System.Diagnostics;
using ComponentDesigner.Nodes;
using ComponentDesigner.Parser;
using ComponentDesigner.Util;

namespace ComponentDesigner;

public static class ValueValidators
{
    public static void PropertyRange(
        IComponentContext context,
        ComponentPropertyValue lowerPropertyValue,
        ComponentPropertyValue upperPropertyValue,
        IDiagnosticBag bag
    )
    {
        if (
            !lowerPropertyValue.Matches(ComponentPropertyValueKind.SingleSyntaxValue) ||
            !upperPropertyValue.Matches(ComponentPropertyValueKind.SingleSyntaxValue)
        ) return;

        if (
            !TryGetIntValue(context, lowerPropertyValue, out var lowerInt) ||
            !TryGetIntValue(context, upperPropertyValue, out var upperInt)
        ) return;

        if (lowerInt > upperInt)
        {
            bag.Add(
                upperPropertyValue.TextSpan.Report(
                    Diagnostic.OutOfRange(
                        lowerPropertyValue.Property,
                        upperPropertyValue.Property,
                        lowerInt,
                        upperInt
                    )
                )
            );
        }
    }

    public static void IntRange(
        IComponentContext context,
        ComponentPropertyValue propertyValue,
        IDiagnosticBag bag,
        int? lower = null,
        int? upper = null
    ) => Range(context, propertyValue, bag, asString: false, lower, upper);

    public static void StringRange(
        IComponentContext context,
        ComponentPropertyValue propertyValue,
        IDiagnosticBag bag,
        int? lower = null,
        int? upper = null
    ) => Range(context, propertyValue, bag, asString: true, lower, upper);

    private delegate void SumFunc(ComponentPropertyValue value, ref int? sum);
    
    public static void Range(
        IComponentContext context,
        ComponentPropertyValue propertyValue,
        IDiagnosticBag bag,
        bool asString,
        int? lower = null,
        int? upper = null
    )
    {
        Debug.Assert(lower.HasValue || upper.HasValue);

        int? sum = null;

        SumFunc sumFunc = asString ? SumStr : SumInt;

        foreach (var value in propertyValue.AsFlattened)
        {
            sumFunc(value, ref sum);
        }

        if (sum is null) return;

        Check(sum.Value);
        

        static void SumStr(ComponentPropertyValue value, ref int? sum)
        {
            switch (value)
            {
                case ComponentPropertyValue.Literal { Value: var str }:
                    sum += str.Length;
                    return;
                case ComponentPropertyValue.Interpolation
                {
                    Info.ConstantValue: { IsSpecified: true, Value: string str }
                }:
                    sum += str.Length;
                    return;
            }
        }

        static void SumInt(ComponentPropertyValue value, ref int? sum)
        {
            switch (value)
            {
                case ComponentPropertyValue.Literal { Value: var str }
                    when int.TryParse(str, out var part):
                case ComponentPropertyValue.Interpolation
                    {
                        Info.ConstantValue: { IsSpecified: true, Value: { } constant }
                    }
                    when int.TryParse(constant.ToString(), out part):
                    sum += part;
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
                                propertyValue.Property,
                                target,
                                lower,
                                upper
                            )
                            : Diagnostic.IntegerOutOfRange(
                                propertyValue.Property,
                                target,
                                lower,
                                upper
                            )
                    )
                );
            }
        }
    }

    private static bool TryGetIntValue(IComponentContext context, ComponentPropertyValue value, out int result)
    {
        switch (value)
        {
            case ComponentPropertyValue.Literal { Value: var str } when int.TryParse(str, out result):
            case ComponentPropertyValue.Interpolation { Info.ConstantValue: { IsSpecified: true, Value: { } constant } }
                when int.TryParse(constant.ToString(), out result):
                return true;

            default:
                result = 0;
                return false;
        }
    }
}