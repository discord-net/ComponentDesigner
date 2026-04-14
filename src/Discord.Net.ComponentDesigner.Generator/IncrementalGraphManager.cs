using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using ComponentDesigner.CSharp;
using Microsoft.CodeAnalysis;

namespace ComponentDesigner;

public sealed class IncrementalGraphManager
{
    private readonly Dictionary<string, int> _keyCounts = [];
    private readonly Dictionary<string, Entry> _graphs = [];
    private readonly HashSet<string> _unprocessed = [];
    
    private readonly record struct Entry(
        GraphParameters Parameters,
        CXComponentGraph Graph
    );

    public UpdatedGraph UpdateGraphDependencies(
        (CXComponentGraph, Compilation) tuple,
        CancellationToken cancellationToken
    )
    {
        var (graph, compilation) = tuple;

        var provider = CSharpCompilationProvider.Get(compilation);

        return new(
            graph.UpdateFromCompilation(provider, cancellationToken),
            provider
        );
    }
    
    public IEnumerable<CXComponentGraph> Update(
        ImmutableArray<GraphParameters> parameters,
        CancellationToken cancellationToken
    )
    {
        var globalKeyCount = 0;
        _keyCounts.Clear();
        _unprocessed.Clear();

        foreach (var entry in _graphs)
        {
            _unprocessed.Add(entry.Key);
        }
        
        foreach (var graphParameters in parameters)
        {
            var key = GetKey(graphParameters);

            if (_graphs.TryGetValue(key, out var entry))
            {
                yield return Update(key, entry, graphParameters);
                continue;
            }

            var graph = CXComponentGraph.Create(
                graphParameters,
                cancellationToken
            );

            _graphs[key] = new(graphParameters, graph);

            yield return graph;
        }

        foreach (var unprocessed in _unprocessed)
        {
            _graphs.Remove(unprocessed);
        }

        CXComponentGraph Update(string key, Entry entry, GraphParameters parameters)
        {
            _unprocessed.Remove(key);
            
            if (entry.Parameters.Equals(parameters))
            {
                // no changes
                return entry.Graph;
            }

            var newGraph = entry.Graph.UpdateFromParameters(parameters, cancellationToken);
            
            _graphs[key] = new(
                parameters,
                newGraph 
            );

            return newGraph;
        }
        
        string GetKey(GraphParameters parameters)
        {
            string key;
            var cx = (CXModel)parameters.CX;

            if (cx.ParentKey is not null)
            {
                _keyCounts.TryGetValue(cx.ParentKey, out var count);
                key = $"{cx.ParentKey}{count}";
                _keyCounts[cx.ParentKey] = count + 1;
            }
            else
            {
                key = $"{cx.ParentKey}{globalKeyCount++}";
            }

            return key;
        }
    }
}