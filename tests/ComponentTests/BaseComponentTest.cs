using System.Diagnostics.CodeAnalysis;
using System.Text;
using Discord.CX;
using Discord.CX.Nodes;
using Discord.CX.Parser.DebugUtils;
using Microsoft.CodeAnalysis;
using Xunit.Abstractions;

namespace UnitTests.ComponentTests;

public abstract class BaseComponentTest(ITestOutputHelper output) : BaseTestWithDiagnostics(output)
{
    protected CXGraph CurrentGraph
    {
        get
        {
            Assert.NotNull(_graph);
            return _graph!;
        }
    }

    private CXGraph? _graph;
    private ComponentContext? _context;
    private IEnumerator<GraphNode>? _nodeEnumerator;

    public void Graph(
        string cx,
        [StringSyntax("csharp")]string? pretext = null,
        bool allowParsingErrors = false,
        GeneratorOptions? options = null,
        [StringSyntax("csharp")]string? additionalMethods = null,
        string testClassName = "TestClass",
        string testFuncName = "Run",
        bool hasInterpolations = true,
        int quoteCount = 3
    )
    {
        if (_graph is not null) EOF();

        _graph = null;
        _context = null;
        _nodeEnumerator = null;

        ClearDiagnostics();

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
        
        var source =
            $$""""
              using Discord;
              using System.Collections.Generic;
              using System.Linq;

              public class {{testClassName}}
              {
                  public void {{testFuncName}}()
                  {
                      {{pretext}}
                      ComponentDesigner.cx(
                          {{cxString.ToString().WithNewlinePadding(4)}}
                      );
                  }
                  {{additionalMethods?.WithNewlinePadding(4)}}
              }
              """";
        
        output.WriteLine($"CX:\n{cxString}");

        var token = new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;
        
        var target = Targets.FromSource(source, token);

        var state = SourceGenerator.CreateGraphState(
            "<global>:0",
            target,
            options ?? GeneratorOptions.Default
        );

        var graph = CXGraph.Create(
            state,
            old: null,
            token
        );
        
        output.WriteLine($"AST:\n{graph.Document.ToStructuralFormat()}");
        output.WriteLine($"DOT:\n{graph.Document.ToDOTFormat()}");

        Assert.Equal(allowParsingErrors, graph.Document.HasErrors);

        graph = graph.Update(target, token);

        PushDiagnostics(graph.Diagnostics);
        
        _graph = graph;
        _nodeEnumerator = _graph.RootNodes.SelectMany(EnumerateNodes).GetEnumerator();
        _context = new(_graph);
    }

    private IEnumerable<GraphNode> EnumerateNodes(GraphNode graphNode)
    {
        yield return graphNode;

        foreach (var attrNode in graphNode.AttributeNodes)
        foreach (var child in EnumerateNodes(attrNode))
            yield return child;

        foreach (var childNode in graphNode.Children)
        foreach (var child in EnumerateNodes(childNode))
            yield return child;
    }

    protected void Validate(bool? hasErrors = null)
    {
        Assert.NotNull(_graph);
        Assert.NotNull(_context);

        var diagnostics = new List<DiagnosticInfo>();
        
        _graph.Validate(_context, diagnostics);
        
        AssertEmptyDiagnostics();

        PushDiagnostics(diagnostics);
        
        if (hasErrors.HasValue)
            Assert.Equal(hasErrors.Value, diagnostics.Any(x => x.Descriptor.DefaultSeverity is DiagnosticSeverity.Error));

    }

    protected void Renders(string? expected = null)
    {
        Assert.NotNull(_graph);
        Assert.NotNull(_context);

        AssertEmptyDiagnostics();

        var result = _graph.Render(_context);

        PushDiagnostics(result.Diagnostics);

        if (expected is not null)
        {
            Assert.NotNull(result.EmittedSource);
            Assert.Equal(expected, result.EmittedSource);
        }
    }


    private GraphNode NextGraphNode()
    {
        Assert.NotNull(_nodeEnumerator);
        Assert.True(_nodeEnumerator.MoveNext());
        return _nodeEnumerator.Current;
    }

    protected override void EOF()
    {
        Assert.NotNull(_nodeEnumerator);
        Assert.False(_nodeEnumerator.MoveNext());

        base.EOF();
    }

    protected T Node<T>() where T : ComponentNode
        => Node<T>(out _);

    protected T Node<T>(out GraphNode graphGraphNode) where T : ComponentNode
    {
        graphGraphNode = NextGraphNode();

        Assert.IsType<T>(graphGraphNode.Inner);

        return (T)graphGraphNode.Inner;
    }
}