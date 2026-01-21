using System;
using System.Collections.Generic;
using Discord.CX.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Discord.CX;

public sealed class ComponentDesignerTarget(
    InterceptableLocation interceptLocation,
    string? parentKey,
    CXDesignerGeneratorState cx,
    ComponentDesignerOptionOverloads overloads
) : IEquatable<ComponentDesignerTarget>
{
    // both compilation and syntax tree is used sparingly; try not to rely on these
    public Compilation Compilation => CX.SemanticModel.Compilation;
    public SyntaxTree SyntaxTree => CX.SyntaxTree;

    public InterceptableLocation InterceptLocation { get; } = interceptLocation;
    public string? ParentKey { get; } = parentKey;
    public CXDesignerGeneratorState CX { get; } = cx;
    
    public ComponentDesignerOptionOverloads Overloads { get; } = overloads;

    public override int GetHashCode()
        => Hash.Combine(
            InterceptLocation,
            ParentKey,
            CX,
            Overloads
        );

    public bool Equals(ComponentDesignerTarget other)
        => InterceptLocation.Equals(other.InterceptLocation) &&
           ParentKey == other.ParentKey &&
           CX.Equals(other.CX) &&
           Overloads.Equals(other.Overloads);
}

public readonly record struct ComponentDesignerOptionOverloads(
    Result<bool> EnableAutoRows,
    Result<bool> EnableAutoTextDisplays
)
{
    public bool IsEmpty => !EnableAutoRows.HasResult && !EnableAutoTextDisplays.HasResult;

    public IEnumerable<DiagnosticInfo> Diagnostics
        => [..EnableAutoRows.Diagnostics, ..EnableAutoTextDisplays.Diagnostics];
}