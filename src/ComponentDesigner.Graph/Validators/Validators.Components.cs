using ComponentDesigner.Nodes;
using ComponentDesigner.Parser;

namespace ComponentDesigner;

public static partial class Validators
{
    public static void ValidateGenericComponent(
        IComponentContext context,
        IComponentNode component,
        ComponentState state,
        IDiagnosticBag bag,
        bool? allowsChildrenInCX = null,
        bool? isParentOfOtherComponents = null
    )
    {
        ValidateComponentTarget(context, component, state, bag);
        ValidateElementStructure(component, state, bag, allowsChildrenInCX, isParentOfOtherComponents);
        ValidateProperties(component, state, bag);
        ReportDiagnosticsForUnknownProperties(component, state, bag);
    }

    public static void ValidateComponentTarget(
        IComponentContext context,
        IComponentNode component,
        ComponentState state,
        IDiagnosticBag bag
    )
    {
        if ((context.Options.Target & component.Target) is ComponentTargetType.None)
        {
            bag.Add(
                Diagnostic.ComponentTargetIsNotAllowed(component, context.Options.Target).At(state)
            );
        }
    }

    public static void ValidateElementStructure(
        IComponentNode component,
        ComponentState state,
        IDiagnosticBag bag,
        bool? allowsChildrenInCX = null,
        bool? isParentOfOtherComponents = null
    )
    {
        if (state.CXNode is not CXElement element) return;

        if (
            (allowsChildrenInCX is false && element.Children.Count > 0) ||
            (isParentOfOtherComponents is false && state.Children.Count > 0)
        )
        {
            bag.Add(
                Diagnostic
                    .ComponentDoesntAllowChildren(component)
                    .At(element.Children)
            );
        }
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
            if (!state.PropertyInfo.TryGet(attribute.Identifier, out _))
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
        foreach (var property in state.PropertyInfo.Properties)
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

    public static bool PropertyMatchesComponents(
        IComponentNode parent,
        ComponentPropertyValue propertyValue,
        IDiagnosticBag bag,
        Func<IComponentNode, bool> matches
    )
    {
        var isValid = true;

        foreach (var innerValue in propertyValue.AsFlattened)
        {
            if (
                innerValue is not ComponentPropertyValue.Component
                {
                    GraphNode:
                    {
                        State.TextSpan: { } textSpan,
                        Component: { } component
                    }
                }
            ) continue;

            if (!matches(component))
            {
                bag.Add(
                    Diagnostic.InvalidChildOfComponent(parent, component).At(textSpan)
                );
                isValid = false;
            }
        }

        return isValid;
    }
}