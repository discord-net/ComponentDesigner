using System;

namespace ComponentDesigner;

public sealed class UpdatedGraph(
    CXComponentGraph graph,
    ICompilationProvider compilationProvider
)  : IEquatable<UpdatedGraph>
{
    public CXComponentGraph Graph { get; } = graph;
    public ICompilationProvider CompilationProvider { get; } = compilationProvider;

    public bool Equals(UpdatedGraph other)
        => Graph.Equals(other.Graph);

    public override bool Equals(object? obj)
        => obj is UpdatedGraph other && Equals(other);

    public override int GetHashCode()
        => Graph.GetHashCode();
}