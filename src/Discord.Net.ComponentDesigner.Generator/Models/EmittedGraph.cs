using System;
using System.Collections.Generic;
using System.Linq;
using ComponentDesigner.CSharp;
using ComponentDesigner.Util;

namespace ComponentDesigner;

public sealed class EmittedGraph : IEquatable<EmittedGraph>
{
    public CXComponentGraph Graph { get; }
    public IReadOnlyList<CSharpRender> Renders { get; }
    public IReadOnlyList<Diagnostic> Diagnostics { get; }
    public ICompilationProvider CompilationProvider { get; }

    public string Source { get; }
    
    public EmittedGraph(
        CXComponentGraph graph,
        IReadOnlyList<CSharpRender> renders,
        IReadOnlyList<Diagnostic> diagnostics,
        ICompilationProvider compilationProvider
    )
    {
        Graph = graph;
        Renders = renders;
        Diagnostics = diagnostics;
        CompilationProvider = compilationProvider;
        Source = string.Join($",{Environment.NewLine}", renders.Select(x => x.Source));
    }

    public bool Equals(EmittedGraph other)
        => Renders.SequenceEqual(other.Renders) &&
           Diagnostics.SequenceEqual(other.Diagnostics);

    public override bool Equals(object? obj)
        => obj is EmittedGraph other && Equals(other);

    public override int GetHashCode()
        => Hash.Combine(
            Renders.Aggregate(0, Hash.Combine),
            Diagnostics.Aggregate(0, Hash.Combine)
        );
}