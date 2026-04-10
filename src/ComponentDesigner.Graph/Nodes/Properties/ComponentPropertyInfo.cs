using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace ComponentDesigner.Nodes;

public sealed class ComponentPropertyInfo
{
    private static readonly
        ConditionalWeakTable<IComponentImplementation, Dictionary<IComponentNode, ComponentPropertyInfo>>
        Table = new();

    public static readonly ComponentPropertyInfo Empty = new([]);
    
    public readonly IReadOnlyList<ComponentProperty> Properties;
    private readonly Dictionary<string, ComponentProperty> _propertyLookup;

    private ComponentPropertyInfo(IComponentNode component, IComponentImplementation implementation)
        : this(GetProperties(component, implementation))
    {
    }

    public ComponentPropertyInfo(IReadOnlyList<ComponentProperty> properties)
    {
        Properties = properties;
        _propertyLookup = CreateMap(Properties);
    }

    private static Dictionary<string, ComponentProperty> CreateMap(
        IReadOnlyList<ComponentProperty> properties
    )
    {
        if (properties.Count is 0) return [];
        
        var map = new Dictionary<string, ComponentProperty>();

        foreach (var property in properties)
        {
            Add(property.Name, property);

            foreach (var alias in property.Aliases)
                Add(alias, property);
        }

        return map;

        void Add(string name, ComponentProperty value)
        {
            if (map.ContainsKey(name)) return;
            map[name] = value;
        }
    }

    public bool TryGet(string name, [MaybeNullWhen(false)] out ComponentProperty property)
        => _propertyLookup.TryGetValue(name, out property);

    private static IReadOnlyList<ComponentProperty> GetProperties(
        IComponentNode component,
        IComponentImplementation implementation
    )
    {
        if (implementation.ComponentExtensionProvider is null)
            return component.Properties;

        return [..component.Properties, ..implementation.ComponentExtensionProvider.GetAdditionalProperties(component)];
    }

    public static ComponentPropertyInfo Get(
        IComponentNode component,
        IComponentImplementation implementation
    )
    {
        if (!Table.TryGetValue(implementation, out var map))
        {
            map = new();
            Table.Add(implementation, map);
        }

        if (!map.TryGetValue(component, out var info))
            map[component] = info = new(component, implementation);

        return info;
    }
}