using Discord.CX.Nodes;
using Discord.CX.Parser;

namespace Discord.CX;

public static class DiagnosticFactory
{
    private enum DiagnosticCode
    {
        UnknownElement = 1,
        UnsupportedSyntaxKindForGraphNode,
        InvalidChildOfComponent,
        RequiredPropertyNotSpecified,
        ComponentDoesntAllowChildren,
        ValueVariantCannotBeGenerated,
        UsingRuntimeValidation,
        TypeMismatch,
        NullValueNotAllowed,
        EmptyValueNotAllowed,
        MissingImplementationForRenderer
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

    extension(CXTextSpan span)
    {
        public Diagnostic Report(DiagnosticDescriptor descriptor)
            => new(span, descriptor);
    }

    extension(ICXNode node)
    {
        public Diagnostic Report(DiagnosticDescriptor descriptor)
            => new(node.Span, descriptor);
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
        public static DiagnosticDescriptor UnknownElement(
            string identifier
        ) => Create(
            DiagnosticSource.Graph,
            DiagnosticCode.UnknownElement,
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

        public static DiagnosticDescriptor RequiredPropertyNotSpecified(
            IComponentNode component,
            ComponentProperty property
        ) => Create(
            DiagnosticSource.Graph,
            DiagnosticCode.RequiredPropertyNotSpecified,
            DiagnosticSeverity.Error,
            $"'{component.Name}' requires the property '{property.Name}' to be specified"
        );

        public static DiagnosticDescriptor ComponentDoesntAllowChildren(
            IComponentNode component
        ) => Create(
            DiagnosticSource.Graph,
            DiagnosticCode.ComponentDoesntAllowChildren,
            DiagnosticSeverity.Error,
            $"'{component.Name}' doesn't allow other components as children"
        );

        public static DiagnosticDescriptor ValueVariantCannotBeGenerated(
            CXValue value
        ) => Diagnostic.ValueVariantCannotBeGenerated(value.GetType().Name);

        public static DiagnosticDescriptor ValueVariantCannotBeGenerated(
            string name
        ) => Create(
            DiagnosticSource.Graph,
            DiagnosticCode.ValueVariantCannotBeGenerated,
            DiagnosticSeverity.Error,
            $"'{name}' is not a valid value"
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
            IComponentRenderer renderer
        ) => Create(
            DiagnosticSource.Renderer,
            DiagnosticCode.MissingImplementationForRenderer,
            DiagnosticSeverity.Error,
            $"The renderer '{renderer.Name}' doesn't provide an implementation for the component '{node.Name}'"
        );
    }
}