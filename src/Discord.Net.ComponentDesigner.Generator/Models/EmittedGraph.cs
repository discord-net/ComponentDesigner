using System;
using System.Collections.Generic;
using System.Linq;
using ComponentDesigner.Util;

namespace ComponentDesigner;

public sealed class EmittedGraph : IEquatable<EmittedGraph>
{
    public CXComponentGraph Graph { get; }
    public string? Source { get; }
    public IReadOnlyList<ComponentDesigner.Diagnostic> Diagnostics { get; }

    public ICompilationProvider CompilationProvider { get; }

    public EmittedGraph(
        CXComponentGraph graph,
        string? source,
        IReadOnlyList<Diagnostic> diagnostics,
        ICompilationProvider compilationProvider
    )
    {
        Graph = graph;
        Source = source;
        Diagnostics = diagnostics;
        CompilationProvider = compilationProvider;
    }

    public bool Equals(EmittedGraph other)
        => Source == other.Source && Diagnostics.SequenceEqual(other.Diagnostics);

    public override bool Equals(object? obj)
        => obj is EmittedGraph other && Equals(other);

    public override int GetHashCode()
        => Hash.Combine(Source, Diagnostics.Aggregate(0, Hash.Combine));
}