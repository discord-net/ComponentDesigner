using System.Text;
using ComponentDesigner.Nodes;
using ComponentDesigner.Parser;

namespace ComponentDesigner;

public static class DiagnosticFactory
{
    private enum DiagnosticCode
    {
        UnknownComponentElement = 1,
        UnsupportedSyntaxKindForGraphNode,
        InvalidChildOfComponent,
        RequiredPropertyNotSpecified,
        RequiredPropertyValueNotSpecified,
        ComponentDoesntAllowChildren,
        ValueVariantCannotBeGenerated,
        UsingRuntimeValidation,
        TypeMismatch,
        NullValueNotAllowed,
        EmptyValueNotAllowed,
        MissingImplementationForRenderer,
        UnknownTextControlElement,
        UnsupportedTextControlElement,
        FeatureAutoTextDisplaysDisabled,
        FeatureAutoActionRowsDisabled,
        ChildSuppliedExclusivePropertyDuplicated,
        UnknownPropertyOfComponent,
        ComponentRequiresAtLeastOneChild,
        OutOfRange,
        PropertyNotAllowed,
        InvalidFunctionalComponent,
        AmbiguousFunctionalComponent,
        NumberOfChildrenOutOfRange,
        TypeNotFound,
        NotAValidEnumVariant,
        InvalidSnowflake,
        TypelessSelectMenu,
        ExpectedAConstantValue,
        SelectMenuDefaultValueMustBeInASelectMenu,
        SelectMenuOptionMustBeInASelectMenu,
        ValueCouldNotBeValidateAndARuntimeValidationCheckWillOccur,
        TooManyChildren,
        DuplicatePropertyValue,
        InvalidPropertyValue,
        InvalidAccessoryComponentOfSection,
        InvalidChildComponentOfSection,
        CannotConvertComponents,
        NotAComponentType,
        InvalidSyntax,
        TypedComponentsAreNotSupported
    }

    private enum DiagnosticSource
    {
        Graph,
        Parser,
        Renderer
    }

    private static string GetSourcePrefix(DiagnosticSource source)
        => source switch
        {
            DiagnosticSource.Graph => "DCMPGPH",
            DiagnosticSource.Parser => "DCXPARS",
            DiagnosticSource.Renderer => "DCRENDR",
            _ => throw new ArgumentOutOfRangeException(nameof(source))
        };

    private static string FormatId(
        DiagnosticSource source,
        DiagnosticCode code
    ) => FormatId(source, (int)code);

    private static string FormatId(
        DiagnosticSource source,
        int code
    ) => $"{GetSourcePrefix(source)}{code.ToString().PadLeft(3, '0')}";

    private static DiagnosticDescriptor Create(
        DiagnosticSource source,
        DiagnosticCode code,
        DiagnosticSeverity severity,
        string title,
        string? message = null
    ) => new(
        FormatId(source, code),
        severity,
        title,
        message
    );

    extension<T>(T locatable) where T : ISourceLocatable
    {
        public Diagnostic Report(DiagnosticDescriptor descriptor)
            => new(locatable.TextSpan, descriptor);
    }

    extension(DiagnosticDescriptor descriptor)
    {
        public Diagnostic At<T>(T locatable) where T : ISourceLocatable => new(locatable.TextSpan, descriptor);
    }

    extension(CXDiagnostic diagnostic)
    {
        public Diagnostic ToNormalDiagnostic()
            => new(
                diagnostic.Span,
                new(
                    FormatId(DiagnosticSource.Parser, (int)diagnostic.Code),
                    diagnostic.Severity,
                    diagnostic.Message
                )
            );
    }

    extension(Diagnostic)
    {
        public static DiagnosticDescriptor UnknownComponentElement(
            string identifier
        ) => Create(
            DiagnosticSource.Graph,
            DiagnosticCode.UnknownComponentElement,
            DiagnosticSeverity.Error,
            $"Unknown component '{identifier}'"
        );

        public static DiagnosticDescriptor UnsupportedSyntaxKindForGraphNode(
            ICXNode node
        ) => Create(
            DiagnosticSource.Graph,
            DiagnosticCode.UnsupportedSyntaxKindForGraphNode,
            DiagnosticSeverity.Error,
            $"Unsupported syntax '{node.GetType().Name}' for graph node"
        );

        public static DiagnosticDescriptor InvalidChildOfComponent(
            IComponentNode parent,
            IComponentNode child
        ) => Create(
            DiagnosticSource.Graph,
            DiagnosticCode.InvalidChildOfComponent,
            DiagnosticSeverity.Error,
            $"'{child.Name}' is not a valid child of '{parent.Name}'"
        );

        public static DiagnosticDescriptor InvalidChildOfComponent(
            IComponentNode parent,
            ICXNode child
        ) => Create(
            DiagnosticSource.Graph,
            DiagnosticCode.InvalidChildOfComponent,
            DiagnosticSeverity.Error,
            $"'{(child switch {
                CXElement element => element.Identifier,
                CXToken token => token.Kind.ToString(),
                _ => child.GetType().Name
            })}' is not a valid child of '{parent.Name}'"
        );


        public static DiagnosticDescriptor RequiredPropertyNotSpecified(
            IComponentNode component,
            ComponentProperty property
        ) => RequiredPropertyNotSpecified(component.Name, property.Name);

        public static DiagnosticDescriptor RequiredPropertyNotSpecified(
            string elementName,
            string propertyName
        ) => Create(
            DiagnosticSource.Graph,
            DiagnosticCode.RequiredPropertyNotSpecified,
            DiagnosticSeverity.Error,
            $"'{elementName}' requires the property '{propertyName}' to be specified"
        );

        public static DiagnosticDescriptor RequiredPropertyValueNotSpecified(
            string propertyName
        ) => Create(
            DiagnosticSource.Graph,
            DiagnosticCode.RequiredPropertyValueNotSpecified,
            DiagnosticSeverity.Error,
            $"'{propertyName}' requires a value"
        );

        public static DiagnosticDescriptor MissingOneOfProperties(
            IComponentNode component,
            params ReadOnlySpan<ComponentProperty> properties
        )
        {
            using var _ = StringBuilder.Pooled(out var sb);

            for (var i = 0; i < properties.Length; i++)
            {
                if (i > 0) sb.Append(" or ");

                var property = properties[i];

                sb.Append('\'').Append(property.Name).Append('\'');
            }

            return Create(
                DiagnosticSource.Graph,
                DiagnosticCode.RequiredPropertyNotSpecified,
                DiagnosticSeverity.Error,
                $"'{component.Name}' requires {sb} to be specified"
            );
        }

        public static DiagnosticDescriptor MissingOneOfProperties(
            IComponentNode component,
            params ReadOnlySpan<ComponentPropertyValue> properties
        )
        {
            using var _ = StringBuilder.Pooled(out var sb);

            for (var i = 0; i < properties.Length; i++)
            {
                if (i > 0) sb.Append(" or ");

                var property = properties[i];

                sb.Append('\'').Append(property.Name).Append('\'');
            }

            return Create(
                DiagnosticSource.Graph,
                DiagnosticCode.RequiredPropertyNotSpecified,
                DiagnosticSeverity.Error,
                $"'{component.Name}' requires {sb} to be specified"
            );
        }

        public static DiagnosticDescriptor ComponentDoesntAllowChildren(
            IComponentNode component
        ) => ComponentDoesntAllowChildren(component.Name);

        public static DiagnosticDescriptor ComponentDoesntAllowChildren(
            string name
        ) => Create(
            DiagnosticSource.Graph,
            DiagnosticCode.ComponentDoesntAllowChildren,
            DiagnosticSeverity.Error,
            $"'{name}' doesn't allow other components as children"
        );

        public static DiagnosticDescriptor ValueVariantCannotBeGenerated(
            CXValue value
        ) => Diagnostic.ValueVariantCannotBeGenerated(value.GetType().Name);

        public static DiagnosticDescriptor ValueVariantCannotBeGenerated(
            ComponentPropertyValue value
        ) => Diagnostic.ValueVariantCannotBeGenerated(value.GetType().Name);

        public static DiagnosticDescriptor ValueVariantCannotBeGenerated(
            string name
        ) => Create(
            DiagnosticSource.Renderer,
            DiagnosticCode.ValueVariantCannotBeGenerated,
            DiagnosticSeverity.Error,
            $"'{name}' is not a valid value"
        );
        
        public static DiagnosticDescriptor ValueVariantCannotBeGenerated(
            ComponentPropertyValue value,
            CSharpValueGenerator generator
        ) => Create(
            DiagnosticSource.Renderer,
            DiagnosticCode.ValueVariantCannotBeGenerated,
            DiagnosticSeverity.Error,
            $"'{value.Kind.ReadableName}' is not a valid value for '{generator.GetType().Name}'"
        );

        public static DiagnosticDescriptor UsingRuntimeValidation(
            string? method
        ) => Create(
            DiagnosticSource.Graph,
            DiagnosticCode.UsingRuntimeValidation,
            DiagnosticSeverity.Warning,
            method is null
                ? "Value will be validated at runtime"
                : $"Value will be validated at runtime using '{method}'"
        );

        public static DiagnosticDescriptor TypeMismatch(
            ICSharpTypeSymbol expected,
            string actual
        ) => Diagnostic.TypeMismatch(expected.ToString(), actual);

        public static DiagnosticDescriptor TypeMismatch(
            string expected,
            ICSharpTypeSymbol actual
        ) => Diagnostic.TypeMismatch(expected, actual.ToString());

        public static DiagnosticDescriptor TypeMismatch(
            ICSharpTypeSymbol expected,
            ICSharpTypeSymbol actual
        ) => Diagnostic.TypeMismatch(expected.ToString(), actual.ToString());

        public static DiagnosticDescriptor TypeMismatch(
            string expected,
            string actual
        ) => Create(
            DiagnosticSource.Graph,
            DiagnosticCode.TypeMismatch,
            DiagnosticSeverity.Error,
            $"Expected type '{expected}' but got '{actual}'"
        );

        public static DiagnosticDescriptor NullValueNotAllowed => Create(
            DiagnosticSource.Graph,
            DiagnosticCode.NullValueNotAllowed,
            DiagnosticSeverity.Error,
            $"'null' is not a valid value"
        );

        public static DiagnosticDescriptor EmptyValueNotAllowed => Create(
            DiagnosticSource.Graph,
            DiagnosticCode.EmptyValueNotAllowed,
            DiagnosticSeverity.Error,
            $"An empty value is not allowed"
        );

        public static DiagnosticDescriptor MissingImplementationForRenderer(
            IComponentNode node,
            IComponentImplementation implementation
        ) => Create(
            DiagnosticSource.Renderer,
            DiagnosticCode.MissingImplementationForRenderer,
            DiagnosticSeverity.Error,
            $"The renderer for '{implementation.Name}' doesn't provide an implementation for the component '{node.Name}'"
        );

        public static DiagnosticDescriptor MissingImplementationForRenderer(
            ComponentProperty property,
            IComponentImplementation implementation
        ) => Create(
            DiagnosticSource.Renderer,
            DiagnosticCode.MissingImplementationForRenderer,
            DiagnosticSeverity.Error,
            $"The renderer for '{implementation.Name}' doesn't provide an implementation for the property '{property.Name}'"
        );

        public static DiagnosticDescriptor UnknownTextControlElement(
            CXElement element
        ) => Create(
            DiagnosticSource.Graph,
            DiagnosticCode.UnknownTextControlElement,
            DiagnosticSeverity.Error,
            $"'{element.Identifier}' is not a known text control element"
        );

        public static DiagnosticDescriptor UnsupportedTextControlElement(
            ICXNode node
        ) => Create(
            DiagnosticSource.Graph,
            DiagnosticCode.UnsupportedTextControlElement,
            DiagnosticSeverity.Error,
            $"'{node.GetType().Name}' is not supported syntax for text control elements"
        );

        public static DiagnosticDescriptor FeatureAutoTextDisplaysDisabled => Create(
            DiagnosticSource.Graph,
            DiagnosticCode.FeatureAutoTextDisplaysDisabled,
            DiagnosticSeverity.Error,
            $"Text must be wrapped in a 'text-display' component",
            "Text related components must be wrapped in a 'text-display' component, you can enable auto text displays to automatically wrap text in a text-display"
        );

        public static DiagnosticDescriptor FeatureAutoActionRowsDisabled => Create(
            DiagnosticSource.Graph,
            DiagnosticCode.FeatureAutoActionRowsDisabled,
            DiagnosticSeverity.Error,
            $"Buttons and Select Menus must be wrapped in an 'action-row' component",
            "Buttons and Select Menus must be wrapped in an 'action-row' component, you can enable auto action rows to automatically wrap them in 'action-row' components"
        );

        public static DiagnosticDescriptor ChildSuppliedExclusivePropertyDuplicated(string propertyName) => Create(
            DiagnosticSource.Graph,
            DiagnosticCode.ChildSuppliedExclusivePropertyDuplicated,
            DiagnosticSeverity.Error,
            $"'{propertyName}' cannot be specified both as an attribute and as children"
        );

        public static DiagnosticDescriptor UnknownPropertyOfComponent(IComponentNode component, string propertyName) =>
            Create(
                DiagnosticSource.Graph,
                DiagnosticCode.UnknownPropertyOfComponent,
                DiagnosticSeverity.Warning,
                $"'{component.Name}' doesn't contain a property named '{propertyName}'"
            );

        public static DiagnosticDescriptor ComponentRequiresAtLeastOneChild(IComponentNode component) => Create(
            DiagnosticSource.Graph,
            DiagnosticCode.ComponentRequiresAtLeastOneChild,
            DiagnosticSeverity.Error,
            $"'{component.Name}' requires at least one child component"
        );

        public static DiagnosticDescriptor ComponentRequiresAtLeastOneChild(CXElement element) => Create(
            DiagnosticSource.Graph,
            DiagnosticCode.ComponentRequiresAtLeastOneChild,
            DiagnosticSeverity.Error,
            $"'{element.Identifier}' requires at least one child component"
        );

        public static DiagnosticDescriptor OutOfRange(
            ComponentProperty lower,
            ComponentProperty upper,
            int lowerValue,
            int upperValue
        ) => Create(
            DiagnosticSource.Graph,
            DiagnosticCode.OutOfRange,
            DiagnosticSeverity.Error,
            $"'{lower.Name}' must be less than or equal to '{upper.Name}' ({lowerValue} <= {upperValue} != true)"
        );

        public static DiagnosticDescriptor IntegerOutOfRange(
            ComponentProperty property,
            int value,
            int? lower = null,
            int? upper = null
        ) => Create(
            DiagnosticSource.Graph,
            DiagnosticCode.OutOfRange,
            DiagnosticSeverity.Error,
            $"'{property.Name}' must be {GetRangeConstraintsMessage(value, lower, upper)}"
        );

        public static DiagnosticDescriptor StringOutOfRange(
            ComponentProperty property,
            int value,
            int? lower = null,
            int? upper = null
        ) => Create(
            DiagnosticSource.Graph,
            DiagnosticCode.OutOfRange,
            DiagnosticSeverity.Error,
            $"'{property.Name}' must be a string with a length {GetRangeConstraintsMessage(value, lower, upper)}"
        );

        private static string GetRangeConstraintsMessage(int value, int? lower, int? upper)
            => (lower, upper) switch
            {
                (not null, null) => $"at least {lower} ({value} >= {lower} != true)",
                (null, not null) => $"at most {upper} ({value} <= {upper} != true)",
                (not null, not null) => $"between {lower} and {upper} ({lower} <= {value} <= {upper} != true)",
                _ => string.Empty
            };

        public static DiagnosticDescriptor ButtonPropertyNotAllowed(ButtonKind kind, ComponentProperty property)
            => Create(
                DiagnosticSource.Graph,
                DiagnosticCode.PropertyNotAllowed,
                DiagnosticSeverity.Error,
                $"'{property.Name}' is not allowed for {kind} buttons"
            );
        
        public static DiagnosticDescriptor SelectMenuPropertyNotAllowed(SelectMenuKind kind, ComponentProperty property)
            => Create(
                DiagnosticSource.Graph,
                DiagnosticCode.PropertyNotAllowed,
                DiagnosticSeverity.Error,
                $"'{property.Name}' is not allowed for {kind} select menus"
            );

        public static DiagnosticDescriptor InvalidFunctionalComponent(
            SearchResult result,
            bool inStaticContext,
            ICSharpTypeSymbol? container
        ) => Create(
            DiagnosticSource.Graph,
            DiagnosticCode.InvalidFunctionalComponent,
            DiagnosticSeverity.Error,
            $"'{result.Symbol}' cannot be used as a functional component because {(
                result.Kind switch {
                    SearchResultKind.DoesntReturnAComponent => "it doesn't return a valid component type",
                    SearchResultKind.NotAccessible => "it isn't accessible in the current context (must be public or internal)",
                    SearchResultKind.NotAMethod => "it isn't a method",
                    SearchResultKind.DoesntMatchStaticContext =>
                        inStaticContext
                            ? $"it requires an instance of '{container}' (non-static method)"
                            : $"it's an static method accessed via an instance of '{container}'",
                    _ => "unknown"
                }
            )}"
        );

        public static DiagnosticDescriptor FunctionalComponentDoesntReturnAComponentType(
            ICSharpMethodSymbol symbol
        ) => Create(
            DiagnosticSource.Graph,
            DiagnosticCode.InvalidFunctionalComponent,
            DiagnosticSeverity.Error,
            $"'{symbol}' cannot be used as a functional component because it doesn't return a valid component type"
        );

        public static DiagnosticDescriptor FunctionalComponentHasDuplicateChildParameter(
            ICSharpMethodSymbol symbol,
            ICSharpParameterSymbol first,
            ICSharpParameterSymbol second
        ) => Create(
            DiagnosticSource.Graph,
            DiagnosticCode.InvalidFunctionalComponent,
            DiagnosticSeverity.Error,
            $"'{symbol}' cannot be used as a functional component because it contains more than one children parameter ('{first.Name}' and '{second.Name}')"
        );

        public static DiagnosticDescriptor AmbiguousFunctionalComponent(SearchResult[] results)
            => Create(
                DiagnosticSource.Graph,
                DiagnosticCode.AmbiguousFunctionalComponent,
                DiagnosticSeverity.Error,
                $"Ambiguous components found: {string.Join(", ", results.Select(x => x.Symbol.ToQualifiedName()))}"
            );

        public static DiagnosticDescriptor OnlyOneChildAllowed(IComponentNode component)
            => Create(
                DiagnosticSource.Graph,
                DiagnosticCode.NumberOfChildrenOutOfRange,
                DiagnosticSeverity.Error,
                $"'{component.Name}' can only contain at most one child"
            );

        public static DiagnosticDescriptor TypeNotFound(string qualifiedName)
            => Create(
                DiagnosticSource.Graph,
                DiagnosticCode.TypeNotFound,
                DiagnosticSeverity.Error,
                $"'{qualifiedName}' couldn't be found in your compilation"
            );

        public static DiagnosticDescriptor NotAValidEnumVariant(string enumName, string variant)
            => Create(
                DiagnosticSource.Graph,
                DiagnosticCode.NotAValidEnumVariant,
                DiagnosticSeverity.Error,
                $"'{variant}' is not a valid enum member of '{enumName}'"
            );

        public static DiagnosticDescriptor InvalidSnowflake(string text)
            => Create(
                DiagnosticSource.Graph,
                DiagnosticCode.InvalidSnowflake,
                DiagnosticSeverity.Error,
                $"'{text}' is not a valid snowflake"
            );

        public static DiagnosticDescriptor TypelessSelectMenu
            => Create(
                DiagnosticSource.Graph,
                DiagnosticCode.TypelessSelectMenu,
                DiagnosticSeverity.Error,
                $"Unknown select menu type (user, role, etc;), please specify the 'type' of the select menu"
            );

        public static DiagnosticDescriptor ExpectedAConstantValue
            => Create(
                DiagnosticSource.Graph,
                DiagnosticCode.ExpectedAConstantValue,
                DiagnosticSeverity.Error,
                $"A constant value is expected"
            );

        public static DiagnosticDescriptor SelectMenuDefaultValueMustBeInASelectMenu
            => Create(
                DiagnosticSource.Graph,
                DiagnosticCode.SelectMenuDefaultValueMustBeInASelectMenu,
                DiagnosticSeverity.Error,
                $"A select menu default value must be within a select menu"
            );

        public static DiagnosticDescriptor SelectMenuOptionMustBeInASelectMenu
            => Create(
                DiagnosticSource.Graph,
                DiagnosticCode.SelectMenuOptionMustBeInASelectMenu,
                DiagnosticSeverity.Error,
                $"A select menu option must be within a select menu"
            );

        public static DiagnosticDescriptor ValueCouldNotBeValidateAndARuntimeValidationCheckWillOccur(
            string expected,
            string value,
            string runtimeCheckMethod
        ) => Create(
            DiagnosticSource.Graph,
            DiagnosticCode.ValueCouldNotBeValidateAndARuntimeValidationCheckWillOccur,
            DiagnosticSeverity.Warning,
            $"'{value}' couldn't be validated against '{expected}', runtime validation check will be performed with '{runtimeCheckMethod}'"
        );

        public static DiagnosticDescriptor TooManyChildren(
            IComponentNode node,
            int maxChildren
        ) => Create(
            DiagnosticSource.Graph,
            DiagnosticCode.TooManyChildren,
            DiagnosticSeverity.Error,
            $"'{node.Name}' can only contain at most {maxChildren} children"
        );

        public static DiagnosticDescriptor TooManyChildren(
            CXElement element,
            int maxChildren
        ) => Create(
            DiagnosticSource.Graph,
            DiagnosticCode.TooManyChildren,
            DiagnosticSeverity.Error,
            $"'{element.Identifier}' can only contain at most {maxChildren} {(maxChildren is 1 ? "child" : "children")}"
        );

        public static DiagnosticDescriptor DuplicatePropertyValue(
            ComponentProperty property
        ) => DuplicatePropertyValue(property.Name);

        public static DiagnosticDescriptor DuplicatePropertyValue(
            string name
        ) => Create(
            DiagnosticSource.Graph,
            DiagnosticCode.DuplicatePropertyValue,
            DiagnosticSeverity.Error,
            $"'{name}' was already specified once"
        );

        public static DiagnosticDescriptor InvalidPropertyValue(
            string property,
            string valueKind
        ) => Create(
            DiagnosticSource.Graph,
            DiagnosticCode.InvalidPropertyValue,
            DiagnosticSeverity.Error,
            $"'{valueKind}' is not a valid value for property '{property}'"
        );

        public static DiagnosticDescriptor InvalidPropertyValue(
            ComponentPropertyValue propertyValue
        ) => InvalidPropertyValue(propertyValue, propertyValue.Property.Kind);
        
        public static DiagnosticDescriptor InvalidPropertyValue(
            ComponentPropertyValue propertyValue,
            ComponentPropertyValueKind expected
        ) => Create(
            DiagnosticSource.Graph,
            DiagnosticCode.InvalidPropertyValue,
            DiagnosticSeverity.Error,
            $"'{propertyValue.Kind.ReadableName}' doesn't match the expected property value of '{expected.ReadableName}'"
        );

        public static DiagnosticDescriptor InvalidAccessoryComponentOfSection(
            IComponentNode componentNode
        ) => Create(
            DiagnosticSource.Graph,
            DiagnosticCode.InvalidAccessoryComponentOfSection,
            DiagnosticSeverity.Error,
            $"'{componentNode.Name}' is not a valid accessory of a section"
        );

        public static DiagnosticDescriptor InvalidChildComponentOfSection(
            IComponentNode componentNode
        ) => Create(
            DiagnosticSource.Graph,
            DiagnosticCode.InvalidChildComponentOfSection,
            DiagnosticSeverity.Error,
            $"'{componentNode.Name}' is not a valid component of a section"
        );

        public static DiagnosticDescriptor NoConversionForComponents(
            ICSharpTypeSymbol from,
            ICSharpTypeSymbol to
        ) => Create(
            DiagnosticSource.Graph,
            DiagnosticCode.CannotConvertComponents,
            DiagnosticSeverity.Error,
            $"No conversion exists for: {from} -> {to}"
        );

        public static DiagnosticDescriptor NoConversionForComponents(
            string from,
            string to
        ) => Create(
            DiagnosticSource.Graph,
            DiagnosticCode.CannotConvertComponents,
            DiagnosticSeverity.Error,
            $"No conversion exists for: {from} -> {to}"
        );

        public static DiagnosticDescriptor NotAComponentType(
            ICSharpTypeSymbol symbol
        ) => Create(
            DiagnosticSource.Graph,
            DiagnosticCode.NotAComponentType,
            DiagnosticSeverity.Error,
            $"'{symbol}' is not a valid component type"
        );

        public static DiagnosticDescriptor TypedComponentsAreNotSupported(
            IComponentImplementation implementation
        ) => Create(
            DiagnosticSource.Graph,
            DiagnosticCode.TypedComponentsAreNotSupported,
            DiagnosticSeverity.Error,
            $"'{implementation.Name}' does not support custom components (interpolations, functional, etc)"
        );
        
        public static DiagnosticDescriptor InvalidSyntaxValue(
            ICXNode syntax
        ) => Create(
            DiagnosticSource.Graph,
            DiagnosticCode.InvalidSyntax,
            DiagnosticSeverity.Error,
            $"'{syntax switch {
                CXToken token => token.Kind.ToString(),
                _ => syntax.GetType().Name
            }}' is not a valid value"
        );
    }
}