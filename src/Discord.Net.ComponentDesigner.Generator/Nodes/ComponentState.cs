using Discord.CX.Parser;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Discord.CX.Nodes;

public class ComponentState
{
    public CXGraph.Node? OwningNode { get; set; }
    public required ICXNode Source { get; init; }

    public bool HasChildren => OwningNode?.Children.Count > 0;

    public IReadOnlyList<CXGraph.Node> Children
        => OwningNode?.Children ?? [];

    public bool IsElement => Source is CXElement;

    public bool IsRootNode => OwningNode?.Parent is null;

    private readonly Dictionary<ComponentProperty, ComponentPropertyValue> _properties = [];

    public ComponentPropertyValue GetProperty(ComponentProperty property)
    {
        //if (!IsElement) return null;

        if (_properties.TryGetValue(property, out var value)) return value;

        var attribute = (Source as CXElement)?
            .Attributes
            .FirstOrDefault(x =>
                property.Name == x.Identifier.Value || property.Aliases.Contains(x.Identifier.Value)
            );

        CXGraph.Node? node = null;

        if (attribute?.Value is CXValue.Element element)
        {
            node = OwningNode?.AttributeNodes
                .FirstOrDefault(x => ReferenceEquals(x.State.Source, element.Value));
        }

        return _properties[property] = new(property, attribute, node);
    }

    public void ReportPropertyNotAllowed(ComponentProperty property, IComponentContext context)
    {
        var propertyValue = GetProperty(property);
        if (propertyValue.IsSpecified)
        {
            context.AddDiagnostic(
                Diagnostics.PropertyNotAllowed,
                propertyValue.Attribute!,
                OwningNode?.Inner.Name,
                propertyValue.Attribute!.Identifier.Value
            );
        }
    }

    public void RequireOneOf(IComponentContext context, params ReadOnlySpan<ComponentProperty> properties)
    {
        if (properties.Length is 0) return;

        if (properties.Length is 1)
        {
            GetProperty(properties[0]).ReportPropertyConfigurationDiagnostics(context, this, optional: false);
            return;
        }

        for (var i = 0; i < properties.Length; i++)
        {
            if (GetProperty(properties[i]).IsSpecified) return;
        }

        var sb = new StringBuilder();

        for (var i = 0; i < properties.Length; i++)
        {
            if (i == properties.Length - 1)
                sb.Append(" or ");

            if (i is not 0) sb.Append('\'');

            sb.Append(properties[i].Name);

            if (i < properties.Length - 1) sb.Append('\'');
        }

        context.AddDiagnostic(
            Diagnostics.MissingRequiredProperty,
            Source,
            OwningNode?.Inner.Name,
            sb.ToString()
        );
    }

    public bool TryGetChildGraphNode(ICXNode node, out CXGraph.Node childNode)
    {
        foreach (var child in Children)
        {
            if (ReferenceEquals(child.State.Source, node))
            {
                childNode = child;
                return true;
            }
        }

        childNode = null!;
        return false;
    }

    public void SubstitutePropertyValue(ComponentProperty property, CXValue value)
    {
        if (!_properties.TryGetValue(property, out var existing))
            _properties[property] = new(property, null) { Value = value };
        else
            _properties[property] = _properties[property] with { Value = value };
    }

    public string RenderProperties(
        ComponentNode node,
        IComponentContext context,
        bool asInitializers = false,
        Predicate<ComponentProperty>? ignorePredicate = null)
    {
        // TODO: correct handling?
        if (Source is not CXElement element) return string.Empty;

        var values = new List<string>();

        foreach (var property in node.Properties)
        {
            if (ignorePredicate?.Invoke(property) is true) continue;

            var propertyValue = GetProperty(property);

            if (propertyValue.CanOmitFromSource) continue;

            var prefix = asInitializers
                ? $"{property.DotnetPropertyName} = "
                : $"{property.DotnetParameterName}: ";

            values.Add($"{prefix}{property.Renderer(context, propertyValue)}");
        }

        return string.Join($",{Environment.NewLine}", values);
    }

    public string RenderInitializer(
        ComponentNode node,
        IComponentContext context,
        Predicate<ComponentProperty>? ignorePredicate = null
    )
    {
        var props = RenderProperties(node, context, asInitializers: true, ignorePredicate);

        if (string.IsNullOrWhiteSpace(props)) return string.Empty;

        return
            $$"""
              {
                  {{props.WithNewlinePadding(4)}}
              }
              """;
    }

    public string RenderChildren(
        IComponentContext context,
        Func<CXGraph.Node, bool>? predicate = null,
        ComponentRenderingOptions options = default
    )
    {
        if (OwningNode is null || !HasChildren) return string.Empty;

        IEnumerable<CXGraph.Node> children = OwningNode.Children;

        if (predicate is not null) children = children.Where(predicate);

        return string.Join(
            $",{Environment.NewLine}",
            children.Select(x => x.Render(context, options))
        );
    }
}