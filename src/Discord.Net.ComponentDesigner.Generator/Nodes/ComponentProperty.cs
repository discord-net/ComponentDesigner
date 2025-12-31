using System;
using Discord.CX.Parser;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Discord.CX.Util;

namespace Discord.CX.Nodes;

public sealed class ComponentProperty : IEquatable<ComponentProperty>
{
    public static ComponentProperty Id => new(
        "id",
        isOptional: true,
        renderer: CXValueGenerator.Integer,
        dotnetPropertyName: "Id"
    );

    public string Name { get; }
    public IImmutableSet<string> Aliases { get; }

    public bool IsOptional { get; }
    public bool RequiresValue { get; }
    public bool Synthetic { get; }
    public string DotnetPropertyName { get; }
    public string DotnetParameterName { get; }
    public CXValueGeneratorDelegate Renderer { get; }

    public IReadOnlyList<PropertyValidator> Validators { get; }

    public ComponentProperty(
        string name,
        bool isOptional = false,
        bool requiresValue = true,
        IEnumerable<string>? aliases = null,
        IEnumerable<PropertyValidator>? validators = null,
        CXValueGeneratorDelegate? renderer = null,
        string? dotnetParameterName = null,
        string? dotnetPropertyName = null,
        bool synthetic = false
    )
    {
        Name = name;
        Aliases = [..aliases ?? []];
        IsOptional = isOptional;
        RequiresValue = requiresValue;
        Synthetic = synthetic;
        DotnetPropertyName = dotnetPropertyName ?? name;
        DotnetParameterName = dotnetParameterName ?? name;
        Renderer = renderer ?? CXValueGenerator.Default;
        Validators = [..validators ?? []];
    }

    public bool Equals(ComponentProperty? other)
    {
        if (other is null) return false;
        
        if (ReferenceEquals(this, other)) return true;

        return
            Name == other.Name &&
            Aliases.SetEquals(other.Aliases) &&
            IsOptional == other.IsOptional &&
            RequiresValue == other.RequiresValue &&
            Synthetic == other.Synthetic &&
            DotnetParameterName == other.DotnetParameterName &&
            DotnetPropertyName == other.DotnetPropertyName &&
            Renderer == other.Renderer &&
            Validators.SequenceEqual(other.Validators);
    }

    public override bool Equals(object? obj)
        => obj is ComponentProperty other && Equals(other);

    public override int GetHashCode()
        => Hash.Combine(
            Name,
            Aliases.Aggregate(0, Hash.Combine),
            IsOptional,
            RequiresValue,
            Synthetic,
            DotnetParameterName,
            DotnetPropertyName,
            Renderer,
            Validators.Aggregate(0, Hash.Combine)
        );

    public static bool operator !=(ComponentProperty a, ComponentProperty? b)
        => !a.Equals(b);
    public static bool operator ==(ComponentProperty a, ComponentProperty? b)
        => a.Equals(b);
}
