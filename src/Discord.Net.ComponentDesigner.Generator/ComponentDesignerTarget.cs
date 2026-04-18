using System;
using ComponentDesigner.Nodes;
using ComponentDesigner.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace ComponentDesigner;

public sealed class ComponentDesignerTarget(
    Compilation compilation,
    InterceptableMethodInfo interceptableMethodInfo,
    CXModel cx,
    GraphOptionsOverloads overloads,
    ComponentTargetType componentTargetType
) : IEquatable<ComponentDesignerTarget>
{
    public Compilation Compilation { get; } = compilation;
    public InterceptableMethodInfo InterceptableMethodInfo { get; } = interceptableMethodInfo;
    public CXModel CX { get; } = cx;
    public GraphOptionsOverloads Overloads { get; } = overloads;
    public ComponentTargetType ComponentTargetType { get; } = componentTargetType;

    public bool Equals(ComponentDesignerTarget? other)
    {
        if (other is null) return false;

        return CX.Equals(other.CX) && 
               InterceptableMethodInfo.Equals(other.InterceptableMethodInfo) &&
               ComponentTargetType == other.ComponentTargetType &&
               Overloads == other.Overloads;
    }

    public override bool Equals(object? obj)
        => obj is ComponentDesignerTarget other && Equals(other);

    public override int GetHashCode()
        => Hash.Combine(CX, InterceptableMethodInfo, ComponentTargetType, Overloads);
}