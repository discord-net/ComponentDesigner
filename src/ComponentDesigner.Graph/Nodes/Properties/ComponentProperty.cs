using System.Collections.Immutable;
using ComponentDesigner.Parser.Util;
using ComponentDesigner.Util;

namespace ComponentDesigner.Nodes;

public sealed class ComponentProperty : IEquatable<ComponentProperty>
{
    public static readonly ComponentProperty Id = new(
        name: "id",
        isOptional: true,
        autoFillMode: PropertyAutoFillMode.String
    );

    public string Name { get; }
    public IImmutableSet<string> Aliases => _aliases ??= [];
    public bool IsOptional { get; }
    public bool RequiresValue { get; }
    public bool IsSynthetic { get; }
    public PropertyAutoFillMode AutoFillMode { get; }
    public IReadOnlyList<string> AutoFillChoices { get; }

    private IImmutableSet<string>? _aliases;

    public ComponentProperty(
        string name,
        IImmutableSet<string>? aliases = null,
        bool isOptional = false,
        bool requiresValue = true,
        bool isSynthetic = false,
        PropertyAutoFillMode autoFillMode = PropertyAutoFillMode.None,
        IReadOnlyList<string>? autoFillChoices = null
    )
    {
        Name = name;
        _aliases = aliases;
        IsOptional = isOptional;
        RequiresValue = requiresValue;
        IsSynthetic = isSynthetic;
        AutoFillMode = autoFillMode;
        AutoFillChoices = autoFillChoices ?? [];
    }

    public bool MatchesName(string name)
        => Name == name || (_aliases is not null && _aliases.Contains(name));

    public bool Equals(ComponentProperty? other)
        => other is not null &&
           Name == other.Name &&
           IsOptional == other.IsOptional &&
           IsSynthetic == other.IsSynthetic &&
           RequiresValue == other.RequiresValue &&
           (_aliases, other._aliases) switch
           {
               (not null, not null) => _aliases.SetEquals(other._aliases),
               (null, null) => true,
               _ => false
           };

    public override bool Equals(object? obj)
        => obj is ComponentProperty other && Equals(other);

    public override int GetHashCode()
        => Hash.Combine(Name, IsOptional, IsSynthetic, RequiresValue, _aliases?.Aggregate(0, Hash.Combine));

    public static bool operator ==(ComponentProperty? left, ComponentProperty? right)
        => left?.Equals(right) ?? right is null;

    public static bool operator !=(ComponentProperty? left, ComponentProperty? right)
        => !(left == right);
}