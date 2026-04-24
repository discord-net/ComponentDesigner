using System.Diagnostics.CodeAnalysis;
using System.Text;
using ComponentDesigner;
using ComponentDesigner.CSharp;
using ComponentDesigner.Nodes;
using ComponentDesigner.Parser.Util;
using ComponentDesigner.Util;
using Discord;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit.Abstractions;

namespace UnitTests.Graph.Components;

public abstract class BaseComponentTest(ITestOutputHelper output) : TestWithDiagnostics(output)
{
    protected CXComponentGraph CurrentGraph
    {
        get
        {
            Assert.NotNull(_graph);
            return _graph;
        }
    }

    private CXComponentGraph? _graph;
    private Compilation? _compilation;
    private IEnumerator<GraphNode>? _nodeEnumerator;

    protected abstract GraphParameters CreateGraphParameters(
        ICompilationProvider compilationProvider,
        ICXModel cxModel,
        IGraphOptions? options
    );

    protected abstract Result<string> EmitGraph(
        CXComponentGraph graph,
        ICompilationProvider compilationProvider,
        CancellationToken cancellationToken = default
    );

    public void Graph(
        string cx,
        [StringSyntax("csharp")] string? pretext = null,
        bool allowParsingErrors = false,
        GeneratorGraphOptions? options = null,
        [StringSyntax("csharp")] string? additionalMembers = null,
        string testClassName = "TestClass",
        string testFuncName = "Run",
        bool hasInterpolations = true,
        int quoteCount = 3
    )
    {
        if (_graph is not null) EOF();

        _graph = null;
        _compilation = null;
        _nodeEnumerator = null;

        var componentTargetType = options?.Target ?? ComponentTargetType.Any;
        
        var source = MakeCSharpSource(
            cx, pretext, quoteCount, hasInterpolations, testClassName, testFuncName,
            additionalMembers, componentTargetType
        );

        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var compilation = _compilation = Compilations
            .Create()
            .AddSyntaxTrees(
                syntaxTree
            );

        var invocation = syntaxTree.GetRoot()
            .DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .FirstOrDefault(x => SourceGenerator.IsComponentDesignerEntryPoint(x, CancellationToken.None));

        Assert.NotNull(invocation);

        var target = SourceGenerator.MapPossibleComponentDesignerEntryPoint(
            compilation.GetSemanticModel(invocation.SyntaxTree),
            invocation
        );

        Assert.NotNull(target);

        var compilationProvider = CSharpCompilationProvider.Get(compilation);

        var graphParameters = CreateGraphParameters(compilationProvider, target.CX, options);

        var graph = CXComponentGraph.Create(graphParameters);

        PushDiagnostics(graph.Diagnostics);

        _graph = graph;
        _nodeEnumerator = graph.RootNodes.SelectMany(EnumerateNodes).GetEnumerator();
    }

    protected void Emits(
        string? expected
    )
    {
        Assert.NotNull(_graph);
        Assert.NotNull(_compilation);
        AssertEmptyDiagnostics();

        var result = EmitGraph(_graph, CSharpCompilationProvider.Get(_compilation));

        PushDiagnostics(result.Diagnostics);

        if (result.HasValue)
        {
            output.WriteLine($"Emitted code:\n{result.Value}");
        }

        if (expected is not null)
        {
            Assert.True(result.HasValue, "emit result should have a value");
            Assert.Equal(expected, result.Value);
        }
        else
        {
            Assert.False(result.HasValue, "emit result was not suppose to have a value");
        }
    }

    protected T Component<T>() where T : IComponentNode
        => Component<T>(out _);

    protected T Component<T>(out GraphNode graphNode) where T : IComponentNode
        => Component<T>(out graphNode, out _);

    protected T Component<T>(out GraphNode graphNode, out ComponentState state)
        where T : IComponentNode
        => Component<T, ComponentState>(out graphNode, out state);

    protected T Component<T, U>(out GraphNode graphNode, out U state)
        where T : IComponentNode
        where U : ComponentState
    {
        Assert.NotNull(_nodeEnumerator);
        Assert.True(_nodeEnumerator.MoveNext(), "expecting another component in the graph");

        graphNode = _nodeEnumerator.Current;

        Assert.IsType<U>(graphNode.State, exactMatch: false);

        state = (U)graphNode.State;

        Assert.IsType<T>(graphNode.Component);

        return (T)graphNode.Component;
    }

    private IEnumerable<GraphNode> EnumerateNodes(GraphNode graphNode)
    {
        yield return graphNode;

        foreach (var childNode in graphNode.Children)
        foreach (var child in EnumerateNodes(childNode))
            yield return child;
    }

    protected override void EOF()
    {
        Assert.NotNull(_nodeEnumerator);
        Assert.False(_nodeEnumerator.MoveNext(), "not all nodes were asserted");
        base.EOF();
    }

    private static string MakeCSharpSource(
        string cx,
        string? pretext,
        int quoteCount,
        bool hasInterpolations,
        string testClassName,
        string testFuncName,
        string? additionalMethods,
        ComponentTargetType target
    )
    {
        var quotes = new string('"', quoteCount);
        var dollar = hasInterpolations ? "$" : string.Empty;
        var pad = hasInterpolations ? new(' ', dollar.Length) : string.Empty;
        var cxString = new StringBuilder();

        cxString.Append(dollar).Append(quotes);

        if (quoteCount >= 3)
        {
            cxString.AppendLine();
            cxString.Append(pad);
        }

        cxString.Append(quoteCount >= 3 ? cx.WithNewlinePadding(pad.Length) : cx);

        if (quoteCount >= 3)
        {
            cxString.AppendLine();
            cxString.Append(pad);
        }

        cxString.Append(quotes);

        return
            $$""""
              using Discord;
              using ComponentDesigner;
              using System.Collections.Generic;
              using System.Linq;

              public class {{testClassName}}
              {
                  public void {{testFuncName}}()
                  {
                      {{pretext?.WithNewlinePadding(8)}}
                      cx.{{target.ToString().ToLowerInvariant()}}(
                          {{cxString.ToString().WithNewlinePadding(12)}}
                      );
                  }
                  {{additionalMethods?.WithNewlinePadding(4)}}
              }
              """";
    }
}