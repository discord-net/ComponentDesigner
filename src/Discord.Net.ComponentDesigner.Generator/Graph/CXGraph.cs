using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Discord.CX.Comparers;
using Discord.CX.Nodes;
using Discord.CX.Nodes.Components;
using Discord.CX.Nodes.Components.Custom;
using Discord.CX.Parser;
using Discord.CX.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Discord.CX;

public sealed class CXGraph : IEquatable<CXGraph>
{
    public string Key { get; }
    public EquatableArray<GraphNode> RootNodes { get; }
    public EquatableArray<DiagnosticInfo> Diagnostics { get; }
    public IReadOnlyDictionary<ICXNode, GraphNode> NodeMap { get; }
    public CXDocument Document { get; }

    public EquatableArray<DesignerInterpolationInfo> InterpolationInfos => _state.CX.InterpolationInfos;

    public Compilation Compilation => _state.Compilation;
    public CXDesignerGeneratorState CX => _state.CX;

    public GeneratorOptions Options => _state.GeneratorOptions;

    private readonly GraphGeneratorState _state;

    private readonly EquatableArray<DiagnosticInfo> _diagnostics;
    private readonly EquatableArray<DiagnosticInfo>? _updateDiagnostics;

    private CXGraph(
        string key,
        EquatableArray<GraphNode> rootNodes,
        EquatableArray<DiagnosticInfo> diagnostics,
        IReadOnlyDictionary<ICXNode, GraphNode> nodeMap,
        GraphGeneratorState state,
        CXDocument document,
        EquatableArray<DiagnosticInfo>? updateDiagnostics = null)
    {
        _state = state;
        Document = document;
        NodeMap = nodeMap;
        Key = key;
        RootNodes = rootNodes;

        _diagnostics = diagnostics;
        _updateDiagnostics = updateDiagnostics;

        Diagnostics = updateDiagnostics is not null
            ? [..diagnostics, ..updateDiagnostics.Value]
            : diagnostics;
    }

    public static CXGraph Create(
        GraphGeneratorState state,
        CXGraph? old,
        CancellationToken token
    )
    {
        // TODO: support inc. parsing
        var reader = new CXSourceReader(
            new CXSourceText.StringSource(state.CX.Designer),
            state
                .CX
                .InterpolationInfos
                .Select(x => new CXTextSpan(
                    x.Span.Start - state.CX.Location.TextSpan.Start,
                    x.Span.Length
                ))
                .ToArray(),
            state.CX.QuoteCount
        );

        var doc = CXParser.Parse(reader, token);

        var parserDiagnostics = doc
            .AllDiagnostics
            .Select(x => new DiagnosticInfo(
                Discord.CX.Diagnostics.CreateParsingDiagnostic(x),
                x.Span
            ));

        if (doc.HasErrors)
        {
            return new CXGraph(
                state.Key,
                [],
                [..parserDiagnostics],
                ImmutableDictionary<ICXNode, GraphNode>.Empty,
                state,
                doc
            );
        }

        var map = new Dictionary<ICXNode, GraphNode>();
        var diagnostics = ImmutableArray.CreateBuilder<DiagnosticInfo>();

        diagnostics.AddRange(parserDiagnostics);

        var context = new GraphInitializationContext(
            state,
            doc,
            [],
            old,
            map,
            diagnostics
        );

        return new CXGraph(
            state.Key,
            [
                ..CreateNodes(
                    doc.RootNodes,
                    parent: null,
                    context
                )
            ],
            diagnostics.ToImmutable(),
            map,
            state,
            doc
        );
    }

    private sealed class GraphUpdateContext : ComponentContext
    {
        public override Compilation Compilation => _target.Compilation;

        public override CXDesignerGeneratorState CX => _target.CX;

        private readonly CXGraph _graph;
        private readonly ComponentDesignerTarget _target;

        public GraphUpdateContext(
            CXGraph graph,
            ComponentDesignerTarget target
        ) : base(graph)
        {
            _graph = graph;
            _target = target;
        }
    }

    public CXGraph Update(
        ComponentDesignerTarget target,
        CancellationToken token
    )
    {
        // no action needed if we're on the same compilation
        if (ReferenceEquals(target.Compilation, _state.Compilation))
        {
            return this;
        }

        var context = new GraphUpdateContext(
            this,
            target
        );

        var diagnostics = new List<DiagnosticInfo>();

        var hasUpdatedState = false;

        var rootNodes = new GraphNode[RootNodes.Count];

        for (var i = 0; i < RootNodes.Count; i++)
        {
            var node = RootNodes[i];
            rootNodes[i] = node.UpdateState(context, diagnostics);
            hasUpdatedState |= !ReferenceEquals(node, rootNodes[i]);
        }

        if (
            hasUpdatedState ||
            diagnostics.Count is not 0 ||
            // any changes to the interpolations should cause a re-render
            !CX.InterpolationInfos.SequenceEqual(
                target.CX.InterpolationInfos,
                DesignerInterpolationInfoComparer.WithoutSpan
            )
        )
        {
            return new CXGraph(
                Key,
                [..rootNodes],
                _diagnostics,
                NodeMap,
                _state.WithCX(context.CX),
                Document,
                [..diagnostics]
            );
        }

        return this;
    }

    public void Validate(IComponentContext context, IList<DiagnosticInfo> diagnostics)
    {
        foreach (var node in RootNodes)
            node.Validate(context, diagnostics);
    }

    public RenderedGraph Render(IComponentContext? context = null, CancellationToken token = default)
    {
        if (RootNodes.IsEmpty)
            return new RenderedGraph(
                Key,
                _state.CX.Designer,
                string.Empty,
                Diagnostics
            );

        context ??= new ComponentContext(this);

        var diagnostics = new List<DiagnosticInfo>(Diagnostics);

        Validate(context, diagnostics);

        var result = RootNodes
            .Select(x => x.Render(context))
            .FlattenAll()
            .Map(x => string.Join($",{Environment.NewLine}", x));

        diagnostics.AddRange(result.Diagnostics);

        return new RenderedGraph(
            Key,
            _state.CX.Designer,
            result.GetValueOrDefault(),
            [..diagnostics]
        );
    }

    private static IEnumerable<GraphNode> CreateNodes(
        IReadOnlyList<CXNode> nodes,
        GraphNode? parent,
        GraphInitializationContext context
    )
    {
        var diagnostics = new List<DiagnosticInfo>();

        for (var i = 0; i < nodes.Count; i++)
        {
            var current = nodes[i];

            if (
                TextControlElement.TryCreate(
                    context,
                    nodes,
                    diagnostics,
                    out var element,
                    out var nodesUsed,
                    startingIndex: i
                )
            )
            {
                if (!context.Options.EnableAutoTextDisplay)
                {
                    diagnostics.Add(
                        Discord.CX.Diagnostics.AutoTextDisplayDisabled,
                        element.Span
                    );
                }
                else
                {
                    // use the first cx node as the node for the auto text
                    var graphNode = new GraphNode(
                        AutoTextDisplayComponentNode.Instance,
                        state: null,
                        children: null,
                        attributeNodes: null,
                        parent
                    );

                    var state = new TextDisplayState(
                        graphNode,
                        current,
                        element
                    );

                    graphNode.State = state;
                    yield return graphNode;
                }

                // advance to the next non text control node, minus one here because the for loop increments by one
                i += nodesUsed - 1;
                continue;
            }

            // use standard create nodes function
            standardNodeCreation:
            foreach (var node in CreateNodes(current, parent, context))
            {
                yield return node;
            }
        }

        foreach (var diagnostic in diagnostics)
            context.Diagnostics.Add(diagnostic);
    }

    private static IEnumerable<GraphNode> CreateNodes(
        CXNode cxNode,
        GraphNode? parent,
        GraphInitializationContext context
    )
    {
        if (context.TryReuse(cxNode, parent, out var reused))
            return [reused];

        switch (cxNode)
        {
            case CXValue.Interpolation interpolation:
                return CreateInterpolationNodes(
                    interpolation,
                    context.Interpolations[interpolation.InterpolationIndex],
                    parent,
                    context
                );
            case CXValue.Multipart multipart:
            {
                var parts = new List<GraphNode>();

                foreach (var token in multipart.Tokens)
                {
                    if (token.InterpolationIndex is { } index)
                    {
                        parts.AddRange(CreateInterpolationNodes(
                            token,
                            context.Interpolations[index],
                            parent,
                            context
                        ));
                    }
                    else
                    {
                        context.Diagnostics.Add(
                            new DiagnosticInfo(
                                Discord.CX.Diagnostics.UnknownComponent(token.Kind.ToString()),
                                cxNode.Span
                            )
                        );
                    }
                }

                return parts;
            }
            case CXElement element:
                return CreateElementNodes(element, parent, context);
            default:
                context.Diagnostics.Add(
                    new(
                        Discord.CX.Diagnostics.UnknownComponent(cxNode.GetType().ToString()),
                        cxNode.Span
                    )
                );
                return [];
        }

        static IEnumerable<GraphNode> CreateElementNodes(
            CXElement element,
            GraphNode? parent,
            GraphInitializationContext context
        )
        {
            if (element.IsFragment)
            {
                return CreateNodes(
                    (IReadOnlyList<CXNode>)element.Children,
                    parent,
                    context
                );
            }

            if (
                !ComponentNode.TryGetNode(element.Identifier, out var componentNode)
            )
            {
                // assume custom component
                componentNode = FunctionalComponentNode.Instance;
            }

            var initContext = new ComponentGraphInitializationContext(
                element,
                parent,
                context
            );

            componentNode.AddGraphNode(initContext);

            return initContext
                .Initializations
                .Select(x =>
                    CreateFromInitialization(x, context, parent)
                )
                .Where(x => x is not null)!;
        }

        static IEnumerable<GraphNode> CreateInterpolationNodes(
            ICXNode cxNode,
            DesignerInterpolationInfo info,
            GraphNode? parent,
            GraphInitializationContext context
        )
        {
            if (InterleavedComponentNode.TryCreate(info.Symbol, context.Compilation, out var inner))
            {
                var node = new GraphNode(
                    inner,
                    state: null,
                    children: null,
                    attributeNodes: null,
                    parent
                );

                var state = inner.Create(
                    new ComponentStateInitializationContext(
                        cxNode,
                        node,
                        [],
                        context
                    ),
                    context.Diagnostics
                );

                if (state is null) return [];

                node.State = state;

                return
                [
                    context.AddToMap(
                        cxNode,
                        node
                    )
                ];
            }

            context.Diagnostics.Add(
                new(
                    Discord.CX.Diagnostics.UnknownComponent(
                        info.Symbol?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                        ?? "<interleaved>"
                    ),
                    cxNode.Span
                )
            );

            return [];
        }
    }

    public static GraphNode? CreateFromInitialization(
        ComponentInitializationGraphNode init,
        GraphInitializationContext context,
        GraphNode? parent
    )
    {
        var nodeChildren = new List<GraphNode>();
        var attributeNodes = new List<GraphNode>();

        var node = new GraphNode(
            init.Node,
            null,
            nodeChildren,
            attributeNodes,
            parent
        );

        List<CXNode> children = [..init.Children];

        var stateContext = new ComponentStateInitializationContext(
            init.CXNode,
            node,
            children,
            context
        );

        var state = init.Node.Create(stateContext, context.Diagnostics);

        if (state is null) return null;

        node.State = state;

        context.AddToMap(
            state.Source,
            node
        );

        if (state?.Source is CXElement { OpeningTag.Attributes: { Count: > 0 } attributes })
        {
            foreach (var attribute in attributes)
            {
                if (attribute.Value is CXValue.Element nestedElement)
                {
                    attributeNodes.AddRange(
                        CreateNodes(
                            nestedElement.Value,
                            node,
                            context
                        )
                    );
                }
            }
        }

        nodeChildren.AddRange(
            CreateNodes(
                children,
                node,
                context
            )
        );

        return node;
    }

    public bool Equals(CXGraph other)
        => Key == other.Key &&
           RootNodes.Equals(other.RootNodes) &&
           Diagnostics.Equals(other.Diagnostics);

    public override bool Equals(object? obj)
        => obj is CXGraph other && Equals(other);

    public override int GetHashCode()
        => Hash.Combine(
            Key,
            RootNodes.Aggregate(0, Hash.Combine),
            Diagnostics
        );
}