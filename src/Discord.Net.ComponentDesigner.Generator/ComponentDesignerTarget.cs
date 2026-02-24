using System;
using ComponentDesigner.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace ComponentDesigner;

public sealed class ComponentDesignerTarget(
    Compilation compilation,
    InterceptableMethodInfo interceptableMethodInfo,
    CXModel cx
) : IEquatable<ComponentDesignerTarget>
{
    public Compilation Compilation { get; } = compilation;
    public InterceptableMethodInfo InterceptableMethodInfo { get; } = interceptableMethodInfo;
    public CXModel CX { get; } = cx;

    public bool Equals(ComponentDesignerTarget? other)
    {
        if (other is null) return false;

        return CX.Equals(other.CX) && InterceptableMethodInfo.Equals(other.InterceptableMethodInfo);
    }

    public override bool Equals(object? obj)
        => obj is ComponentDesignerTarget other && Equals(other);

    public override int GetHashCode()
        => Hash.Combine(CX, InterceptableMethodInfo);
}