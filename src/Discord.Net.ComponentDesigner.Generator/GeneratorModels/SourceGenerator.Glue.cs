using System;
using Discord.CX.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Discord.CX;

partial class SourceGenerator
{
    public sealed class Glue(
        Compilation compilation,
        string key,
        InterceptableLocation interceptLocation,
        LocationInfo location,
        bool usesDesigner,
        SyntaxTree syntaxTree,
        ComponentDesignerOptionOverloads overloads,
        CXKind kind
    ) : IEquatable<Glue>
    {
        public Compilation Compilation { get; } = compilation;
        public string Key { get; init; } = key;
        public InterceptableLocation InterceptLocation { get; init; } = interceptLocation;
        public LocationInfo Location { get; init; } = location;
        public bool UsesDesigner { get; init; } = usesDesigner;
        public SyntaxTree SyntaxTree { get; init; } = syntaxTree;
        public ComponentDesignerOptionOverloads Overloads { get; } = overloads;
        public CXKind Kind { get; } = kind;

        public bool Equals(Glue other)
            => Key == other.Key &&
               InterceptLocation.Equals(other.InterceptLocation) &&
               Location.Equals(other.Location) &&
               UsesDesigner == other.UsesDesigner &&
               Kind == other.Kind;

        public override bool Equals(object? obj)
            => obj is Glue other && Equals(other);

        public override int GetHashCode()
            => Hash.Combine(Key, InterceptLocation, Location, UsesDesigner, Kind);
    }
}