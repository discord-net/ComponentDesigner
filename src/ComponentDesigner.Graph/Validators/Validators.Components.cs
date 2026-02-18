using ComponentDesigner.Nodes;
using ComponentDesigner.Parser;

namespace ComponentDesigner;

public static partial class Validators
{
    public static void ValidateGenericComponent(IComponentNode component, ComponentState state, IDiagnosticBag bag)
    {
        ValidateElementStructure(component, state, bag);
        ValidateProperties(component, state, bag);
        ReportDiagnosticsForUnknownProperties(component, state, bag);
    }

    public static void ValidateElementStructure(
        IComponentNode component,
        ComponentState state,
        IDiagnosticBag bag,
        bool? allowsChildrenInCXOverride = null,
        bool? isParentOverride = null
    )
    {
        if (state.CXNode is not CXElement element) return;

        var allowsChildrenInCX = allowsChildrenInCXOverride ?? component.AllowChildrenInCX;
        var isParent = isParentOverride ?? component.IsParentOfOtherComponents;

        if (!allowsChildrenInCX && element.Children.Count > 0)
        {
            bag.Add(
                element.Children.Report(
                    Diagnostic.ComponentDoesntAllowChildren(component)
                )
            );
        }
        else if (!isParent && state.Children.Count > 0)
        {
            bag.Add(
                element.Children.Report(
                    Diagnostic.ComponentDoesntAllowChildren(component)
                )
            );
        }
        
        // if (!allowsChildrenInCX && !isParent && element.Children.Count > 0)
        // {
        //     bag.Add(
        //         element.Children.Report(
        //             Diagnostic.ComponentDoesntAllowChildren(component)
        //         )
        //     );
        // }
    }

    public static void ReportDiagnosticsForUnknownProperties(
        IComponentNode component,
        ComponentState state,
        IDiagnosticBag bag
    )
    {
        if (state.CXNode is not CXElement element) return;

        foreach (var attribute in element.Attributes)
        {
            if (component.Properties.All(x => !x.MatchesName(attribute.Identifier)))
            {
                bag.Add(
                    attribute.Report(
                        Diagnostic.UnknownPropertyOfComponent(component, attribute.Identifier)
                    )
                );
            }
        }
    }

    public static void ValidateProperties(
        IComponentNode component,
        ComponentState state,
        IDiagnosticBag bag
    )
    {
        foreach (var property in component.Properties)
        {
            ValidateProperty(component, state.GetPropertyValue(property), bag);
        }
    }

    public static void ValidateProperty(
        IComponentNode component,
        ComponentPropertyValue propertyValue,
        IDiagnosticBag bag,
        bool? isOptional = null,
        bool? requiresValue = null
    )
    {
        var optional = isOptional ?? propertyValue.IsOptional;
        var requireValue = requiresValue ?? propertyValue.RequiresValue;

        if (
            (!optional && !propertyValue.IsSpecified) ||
            (requireValue && propertyValue is { HasValue: false, IsSpecified: true })
        )
        {
            bag.Add(
                propertyValue.TextSpan.Report(
                    Diagnostic.RequiredPropertyNotSpecified(component, propertyValue.Property)
                )
            );
        }
    }

    public static void RequireOneOf(
        IComponentNode component,
        ComponentState state,
        IDiagnosticBag bag,
        params ReadOnlySpan<ComponentPropertyValue> properties
    )
    {
        switch (properties.Length)
        {
            case 0: return;
            case 1:
                ValidateProperty(component, properties[0], bag, isOptional: false);
                return;
            default:
                for (var i = 0; i < properties.Length; i++)
                {
                    if (properties[0].IsSpecified) return;
                }

                bag.Add(
                    state.ElementIdentifierTextSpanOrBetter.Report(
                        Diagnostic.MissingOneOfProperties(component, properties)
                    )
                );
                return;
        }
    }
}