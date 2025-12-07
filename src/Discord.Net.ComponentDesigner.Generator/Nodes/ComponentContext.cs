using System;
using Discord.CX.Parser;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Linq;
using Discord.CX.Nodes.Components;

namespace Discord.CX.Nodes;

public sealed class ComponentContext : IComponentContext
{
    private sealed record DiagnosticScope(
        List<Diagnostic> Bag,
        DiagnosticScope? Parent,
        ComponentContext Context
    ) : IDisposable
    {
        public void Dispose()
        {
            if (Parent is null) return;
            Context._scope = Parent;
        }
    }
    
    public KnownTypes KnownTypes => Compilation.GetKnownTypes();
    public Compilation Compilation => _graph.Manager.Compilation;

    public bool HasErrors => GlobalDiagnostics.Any(x => x.Severity is DiagnosticSeverity.Error);

    public List<Diagnostic> GlobalDiagnostics { get; init; } = [];

    public ComponentTypingContext RootTypingContext { get; }
    
    private readonly CXGraph _graph;

    private DiagnosticScope _scope;

    public ComponentContext(CXGraph graph, ComponentTypingContext? typingContext = null)
    {
        _graph = graph;
        _scope = new(GlobalDiagnostics, null, this);
        
        RootTypingContext = typingContext ?? ComponentTypingContext.Default;
    }

    public IDisposable CreateDiagnosticScope(List<Diagnostic> bag)
        => _scope = new(bag, _scope, this);

    public string GetDesignerValue(int index, string? type = null)
        => type is not null ? $"designer.GetValue<{type}>({index})" : $"designer.GetValueAsString({index})";

    public Location GetLocation(TextSpan span)
        => _graph.GetLocation(span);
    
    public DesignerInterpolationInfo GetInterpolationInfo(CXToken token)
        => GetInterpolationInfo(_graph.Manager.Document.GetInterpolationIndex(token));
    public DesignerInterpolationInfo GetInterpolationInfo(int index) => _graph.Manager.InterpolationInfos[index];

    public void AddDiagnostic(Diagnostic diagnostics)
    {
        _scope.Bag.Add(diagnostics);
    }

    IReadOnlyList<Diagnostic> IComponentContext.GlobalDiagnostics => GlobalDiagnostics;

}
