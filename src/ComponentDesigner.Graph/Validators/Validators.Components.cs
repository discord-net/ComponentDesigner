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
        bool? isOptionalOverload = null,
        bool? requiresValueOverload = null
    )
    {
        var isOptional = isOptionalOverload ?? propertyValue.Property.IsOptional;
        var requiresValue = requiresValueOverload ?? propertyValue.Property.RequiresValue;

        if (propertyValue.IsNone)
        {
            // optional property doesn't have a value, OK
            if (isOptional) return;

            if (propertyValue.IsAttributeNameOnly)
            {
                // non-optional property specified by name only and it doesn't require a value, OK
                if (!requiresValue) return;

                // missing required value
                bag.Add(
                    Diagnostic
                        .RequiredPropertyValueNotSpecified(propertyValue)
                        .At(propertyValue)
                );
                return;
            }
            
            // property is not specified at all, and its not optional
            DiagnosticDescriptor diagnostic;

            if (propertyValue.Property.IsFromChildren)
            {
                diagnostic = propertyValue.Property.ValueCardinalityOfMany
                    ? Diagnostic.ComponentRequiresAtLeastOneChild(component)
                    : Diagnostic.ComponentRequiresOneChild(component);
            }
            else
            {
                diagnostic = Diagnostic.RequiredPropertyNotSpecified(component, propertyValue.Property);
            }

            bag.Add(
                diagnostic.At(propertyValue)
            );
            return;
        }

        if (!propertyValue.IsValidBySpec)
        {
            bag.Add(
                Diagnostic
                    .InvalidPropertyValue(propertyValue)
                    .At(propertyValue)
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
                ValidateProperty(component, properties[0], bag, isOptionalOverload: false);
                return;
            default:
                for (var i = 0; i < properties.Length; i++)
                {
                    if (properties[0].IsSome) return;
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