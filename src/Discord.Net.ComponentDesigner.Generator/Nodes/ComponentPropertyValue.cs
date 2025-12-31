using Discord.CX.Parser;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;

namespace Discord.CX.Nodes;

public sealed record ComponentPropertyValue(
    ComponentProperty Property,
    CXAttribute? Attribute,
    TextSpan SourceSpan,
    GraphNode? GraphNode = null
) : IComponentPropertyValue
{
    public TextSpan Span
    {
        get
        {
            if (Value is not null) return Value.Span;

            if (Attribute is not null) return Attribute.Span;

            return SourceSpan;
        }
    }
    
    private CXValue? _value;

    public CXValue? Value
    {
        get => _value ??= Attribute?.Value;
        init => _value = value;
    }

    public bool IsSpecified => Attribute is not null || HasValue;

    public bool HasValue => Value is not null;

    public bool IsAttributeValue => Attribute is not null;

    public bool RequiresValue => Property.RequiresValue;

    public bool IsOptional => Property.IsOptional;
    public string PropertyName => Property.Name;

    public bool CanOmitFromSource => Property.Synthetic || (Property.IsOptional && !IsSpecified);

    public bool TryGetLiteralValue(out string value)
    {
        switch (Value)
        {
            case CXValue.Scalar scalar:
                value = scalar.Value;
                return true;
            case CXValue.StringLiteral { HasInterpolations: false } literal:
                value = literal.Tokens.ToString();
                return true;

            default:
                value = string.Empty;
                return false;
        }
    }

    public void ReportPropertyConfigurationDiagnostics(
        IComponentContext context,
        ComponentState state,
        IList<DiagnosticInfo> diagnostics,
        bool? optional = null,
        bool? requiresValue = null
    )
    {
        var isOptional = optional ?? Property.IsOptional;

        if (!isOptional && !IsSpecified)
        {
            diagnostics.Add(
                Diagnostics.MissingRequiredProperty(state.GraphNode?.Inner.Name, Property.Name),
                state.Source
            );
        }

        if (requiresValue.HasValue && Attribute is not null && Value is null)
        {
            diagnostics.Add(
                Diagnostics.MissingRequiredProperty(
                    state.GraphNode?.Inner.Name,
                    Property.Name
                ),
                Attribute ?? state.Source
            );
        }
    }
}