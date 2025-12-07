using Discord.CX;
using Discord.CX.Nodes;
using Microsoft.CodeAnalysis;

namespace UnitTests.ComponentTests;

public abstract class BaseComponentTest : BaseTestWithDiagnostics
{
    protected CXGraph CurrentGraph => _graph!.Value;

    private CXGraph? _graph;
    private ComponentContext? _context;
    private IEnumerator<CXGraph.Node>? _nodeEnumerator;

    public void Graph(
        string cx,
        string? pretext = null,
        bool allowParsingErrors = false,
        GeneratorOptions? options = null,
        string? additionalMethods = null,
        string testClassName = "TestClass",
        string testFuncName = "Run"
    )
    {
        if (_graph is not null) EOF();

        _graph = null;
        _context = null;
        _nodeEnumerator = null;

        ClearDiagnostics();

        var source =
            $$""""
              using Discord;

              public class {{testClassName}}
              {
                  public void {{testFuncName}}()
                  {
                      {{pretext}}
                      ComponentDesigner.cx(
                          $"""
                           {{cx.WithNewlinePadding(5)}}
                           """
                      );
                  }
                  {{additionalMethods?.WithNewlinePadding(4)}}
              }
              """";

        var target = Targets.FromSource(source);

        var manager = CXGraphManager.Create(
            new SourceGenerator(),
            "<global>:0",
            target,
            options ?? GeneratorOptions.Default,
            CancellationToken.None
        );

        Assert.Equal(allowParsingErrors, manager.Document.HasErrors);

        _graph = manager.Graph;
        _nodeEnumerator = _graph.Value.RootNodes.SelectMany(EnumerateNodes).GetEnumerator();
        _context = new(_graph.Value);
    }

    private IEnumerable<CXGraph.Node> EnumerateNodes(CXGraph.Node node)
    {
        yield return node;

        foreach (var attrNode in node.AttributeNodes)
        foreach (var child in EnumerateNodes(attrNode))
            yield return child;

        foreach (var childNode in node.Children)
        foreach (var child in EnumerateNodes(childNode))
            yield return child;
    }

    protected void Validate(bool? hasErrors = null)
    {
        Assert.NotNull(_graph);
        Assert.NotNull(_context);

        _graph.Value.Validate(_context);

        if (hasErrors.HasValue)
            Assert.Equal(hasErrors.Value, _graph.Value.HasErrors);

        AssertEmptyDiagnostics();

        PushDiagnostics([.._context.GlobalDiagnostics, .._graph.Value.Diagnostics]);
    }

    protected void Renders(string? expected = null)
    {
        Assert.NotNull(_graph);
        Assert.NotNull(_context);

        AssertEmptyDiagnostics();

        var result = _graph.Value.Render(_context);

        PushDiagnostics([.._context.GlobalDiagnostics, .._graph.Value.Diagnostics]);

        if (expected is not null) Assert.Equal(expected, result);
    }


    private CXGraph.Node NextGraphNode()
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

    protected T Node<T>(out CXGraph.Node graphNode) where T : ComponentNode
    {
        graphNode = NextGraphNode();

        Assert.IsType<T>(graphNode.Inner);

        return (T)graphNode.Inner;
    }
}