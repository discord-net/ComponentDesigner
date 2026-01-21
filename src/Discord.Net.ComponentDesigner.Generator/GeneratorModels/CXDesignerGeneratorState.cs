using System;
using Discord.CX.Util;
using Microsoft.CodeAnalysis;

namespace Discord.CX;

public sealed class CXDesignerGeneratorState(
    string designer,
    LocationInfo location,
    int quoteCount,
    bool usesDesignerParameter,
    EquatableArray<DesignerInterpolationInfo> interpolationInfos,
    SemanticModel semanticModel,
    SyntaxTree syntaxTree,
    CXKind kind
) : IEquatable<CXDesignerGeneratorState>
{
    public CXKind Kind { get; init; } = kind;
    public string Designer { get; init; } = designer;
    public LocationInfo Location { get; init; } = location;
    public int QuoteCount { get; init; } = quoteCount;
    public bool UsesDesignerParameter { get; init; } = usesDesignerParameter;
    public EquatableArray<DesignerInterpolationInfo> InterpolationInfos { get; init; } = interpolationInfos;
    public SemanticModel SemanticModel { get; } = semanticModel;
    public SyntaxTree SyntaxTree { get; } = syntaxTree;
    

    public bool Equals(CXDesignerGeneratorState other)
        => Designer == other.Designer &&
           Location.Equals(other.Location) &&
           QuoteCount == other.QuoteCount &&
           UsesDesignerParameter == other.UsesDesignerParameter &&
           InterpolationInfos.Equals(other.InterpolationInfos) &&
           Kind == other.Kind;

    public override bool Equals(object? obj)
        => obj is CXDesignerGeneratorState other && Equals(other);

    public override int GetHashCode()
        => Hash.Combine(Designer, Location, QuoteCount, UsesDesignerParameter, InterpolationInfos, Kind);
}