using System.Collections.Generic;
using System.Linq;
using Discord.CX.Parser;
using Microsoft.CodeAnalysis;

namespace Discord.CX.Nodes.Components.Custom;

public sealed class ProviderComponentNode : ComponentNode
{
    private readonly INamedTypeSymbol _stateSymbol;
    private readonly INamedTypeSymbol _providerSymbol;
    public override string Name => $"<provider {_providerSymbol.ToDisplayString()}>";

    public override IReadOnlyList<ComponentProperty> Properties { get; }

    public ProviderComponentNode(
        INamedTypeSymbol stateSymbol,
        INamedTypeSymbol providerSymbol,
        Compilation compilation
    )
    {
        _stateSymbol = stateSymbol;
        _providerSymbol = providerSymbol;

        var properties = new List<ComponentProperty>();

        foreach (var property in _stateSymbol.GetMembers().OfType<IPropertySymbol>())
        {
            var attribute = property
                .GetAttributes()
                .FirstOrDefault(x =>
                    compilation
                        .GetKnownTypes()
                        .CXPropertyAttributeType!
                        .Equals(x.AttributeClass, SymbolEqualityComparer.Default)
                );

            if (attribute is null) continue;

            var attributeProperties = attribute
                .NamedArguments
                .ToDictionary(x => x.Key, x => x.Value);

            var isOptional = attributeProperties.TryGetValue("IsOptional", out var val) &&
                             ((val.Value as bool?) ?? false);

            var propName = attributeProperties.TryGetValue("Name", out val)
                ? (val.Value as string) ?? property.Name
                : property.Name;

            var aliases = attributeProperties
                .TryGetValue("Aliases", out val)
                ? val.Values
                    .Select(x => (x.Value as string))
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToArray()
                : [];

            properties.Add(
                new ComponentProperty(
                    propName,
                    isOptional,
                    aliases: aliases!,
                    renderer: Renderers.CreateRenderer(property.Type)
                )
            );
        }

        Properties = properties;
    }

    

    public override string Render(ComponentState state,
        IComponentContext context, ComponentRenderingOptions options) =>
        $"{_providerSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.Render({CreateProviderState(state, context)})";


    private string CreateProviderState(
        ComponentState state,
        IComponentContext context
    ) => $"new {_stateSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}(){state.RenderInitializer(this, context)}";
}