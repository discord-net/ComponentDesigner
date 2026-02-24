using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace ComponentDesigner;

public sealed class ComponentDesignerTarget(
    Compilation compilation,
    InterceptableLocation interceptableLocation,
    CXModel cx
) : IEquatable<ComponentDesignerTarget>
{
    public Compilation Compilation { get; } = compilation;
    public InterceptableLocation InterceptableLocation { get; } = interceptableLocation;
    public CXModel CX { get; } = cx;

    public bool Equals(ComponentDesignerTarget? other)
    {
        if (other is null) return false;

        return CX.Equals(other.CX) && InterceptableLocation.Equals(other.InterceptableLocation);
    }
}