using Discord.CX.Nodes;
using Discord.CX.Nodes.Components;
using Discord.CX.Parser;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Discord.CX;

public readonly struct CXGraph
{
    public bool HasErrors => Diagnostics.Any(x => x.Severity is DiagnosticSeverity.Error);

    public IReadOnlyList<Diagnostic> Diagnostics
        => [.._diagnostics, ..RootNodes.SelectMany(x => x.Diagnostics)];

    public readonly CXGraphManager Manager;
    public readonly ImmutableArray<Node> RootNodes;
    public readonly IReadOnlyDictionary<ICXNode, Node> NodeMap;

    private readonly ImmutableArray<Diagnostic> _diagnostics;

    public CXGraph(
        CXGraphManager manager,
        ImmutableArray<Node> rootNodes,
        ImmutableArray<Diagnostic> diagnostics,
        IReadOnlyDictionary<ICXNode, Node> nodeMap
    )
    {
        Manager = manager;
        RootNodes = rootNodes;
        _diagnostics = diagnostics;
        NodeMap = nodeMap;
    }

    public Location GetLocation(ICXNode node) => GetLocation(Manager, node);
    public Location GetLocation(TextSpan span) => GetLocation(Manager, span);

    public static Location GetLocation(CXGraphManager manager, ICXNode node)
        => GetLocation(manager, node.Span);

    public static Location GetLocation(CXGraphManager manager, TextSpan span)
        => manager.SyntaxTree.GetLocation(span);

    public CXGraph Update(
        CXGraphManager manager,
        IncrementalParseResult parseResult,
        CXDoc document
    )
    {
        /*
         * Update semantics:
         *
         * We try to reuse any ComponentNodes who's source is a reused node within the 'parseResult' AND contain no
         * diagnostics. Any nodes with errors are not reused.
         */

        if (manager == Manager) return this;

        var map = new Dictionary<ICXNode, Node>();
        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();

        var rootNodes = ImmutableArray.CreateBuilder<Node>();

        var context = new GraphInitializationContext(
            manager,
            parseResult.ReusedNodes,
            this,
            map,
            diagnostics,
            parseResult
        );

        foreach (var cxNode in document.RootNodes)
        {
            rootNodes.AddRange(
                CreateNodes(
                    cxNode,
                    null,
                    context
                )
            );
        }

        return new(manager, rootNodes.ToImmutable(), diagnostics.ToImmutable(), map);
    }

    public static CXGraph Create(
        CXGraphManager manager
    )
    {
        var map = new Dictionary<ICXNode, Node>();
        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();

        var context = new GraphInitializationContext(
            manager,
            [],
            null,
            map,
            diagnostics,
            null
        );

        var rootNodes = manager.Document
            .RootNodes
            .SelectMany(x =>
                CreateNodes(
                    x,
                    null,
                    context
                )
            )
            .ToImmutableArray();

        return new(manager, rootNodes!, diagnostics.ToImmutable(), map);
    }

    public readonly record struct GraphInitializationContext(
        CXGraphManager Manager,
        IReadOnlyList<ICXNode> ReusedNodes,
        CXGraph? OldGraph,
        Dictionary<ICXNode, Node> Map,
        ImmutableArray<Diagnostic>.Builder Diagnostics,
        IncrementalParseResult? IncrementalParseResult
    )
    {
        public GeneratorOptions Options => Manager.Options;
        public Compilation Compilation => Manager.Compilation;
        public bool IsIncremental => HasOldGraph && HasIncrementalParseResult;

        public bool HasIncrementalParseResult => IncrementalParseResult is not null;
        public bool HasOldGraph => OldGraph is not null;

        public bool TryReuse(ICXNode node, Node? parent, out Node graphNode)
        {
            if (
                IsIncremental &&
                ReusedNodes.Contains(node) &&
                OldGraph!.Value.NodeMap.TryGetValue(node, out graphNode) &&
                graphNode.Diagnostics.Count is 0
            )
            {
                graphNode = AddToMap(node, graphNode.Reuse(parent, IncrementalParseResult!.Value, Manager));
                return true;
            }

            graphNode = null!;
            return false;
        }

        public Node AddToMap(ICXNode? node, Node graphNode)
        {
            if (node is null) return graphNode;

            return Map[node] = graphNode;
        }
    }

    private static IEnumerable<Node> CreateNodes(
        CXNode cxNode,
        Node? parent,
        GraphInitializationContext context
    )
    {
        if (context.TryReuse(cxNode, parent, out var existing))
        {
            return [existing];
        }

        switch (cxNode)
        {
            case CXValue.Interpolation interpolation:
            {
                var info = context.Manager.InterpolationInfos[interpolation.InterpolationIndex];
                return FromInterpolation(cxNode, info);
            }
            case CXValue.Multipart multipart:
                var parts = new List<Node>();
                foreach (var token in multipart.Tokens)
                {
                    if (token.InterpolationIndex is { } index)
                        parts.AddRange(FromInterpolation(token, context.Manager.InterpolationInfos[index]));
                    else
                    {
                        context.Diagnostics.Add(
                            Diagnostic.Create(
                                CX.Diagnostics.UnknownComponent,
                                GetLocation(context.Manager, cxNode),
                                token.Kind
                            )
                        );
                    }
                }

                return parts;
            case CXElement element:
            {
                if (element.IsFragment)
                {
                    return element.Children.SelectMany(x =>
                        CreateNodes(
                            x,
                            parent,
                            context
                        )
                    );
                }

                if (
                    !ComponentNode.TryGetNode(element.Identifier, out var componentNode) &&
                    !ComponentNode.TryGetProviderNode(
                        context.Compilation.GetSemanticModel(context.Manager.SyntaxTree),
                        context.Manager.ArgumentExpressionSyntax.SpanStart,
                        element.Identifier,
                        out componentNode
                    )
                )
                {
                    context.Diagnostics.Add(
                        Diagnostic.Create(
                            CX.Diagnostics.UnknownComponent,
                            GetLocation(context.Manager, element),
                            element.Identifier
                        )
                    );

                    return [];
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
                        CreateNodeFromInitialization(
                            x,
                            context,
                            parent
                        )
                    );
            }
            default:
                context.Diagnostics.Add(
                    Diagnostic.Create(
                        CX.Diagnostics.UnknownComponent,
                        GetLocation(context.Manager, cxNode),
                        cxNode.GetType()
                    )
                );
                return [];
        }

        IEnumerable<Node> FromInterpolation(ICXNode node, DesignerInterpolationInfo info)
        {
            if (
                InterleavedComponentNode.TryCreate(
                    info.Symbol,
                    context.Compilation,
                    out var inner
                )
            )
            {
                var state = inner.Create(new(node, context.Options, []));

                if (state is null) return [];

                return
                [
                    context.AddToMap(
                        node,
                        new(
                            inner,
                            state,
                            [],
                            [],
                            parent
                        )
                    )
                ];
            }

            return [];
        }
    }

    public static CXGraph.Node CreateNodeFromInitialization(
        ComponentInitializationGraphNode initialization,
        GraphInitializationContext context,
        Node? parent
    )
    {
        var state = initialization.State;

        var nodeChildren = new List<Node>();
        var attributeNodes = new List<Node>();

        var node = state.OwningNode = context.AddToMap(
            state.Source,
            new(
                initialization.Node,
                state,
                nodeChildren,
                attributeNodes,
                parent
            )
        );


        if (state.Source is CXElement { Attributes: { Count: > 0 } attributes })
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
            initialization
                .Children
                .SelectMany(x => CreateNodes(
                        x,
                        node,
                        context
                    )
                )
        );

        return node;
    }

    public void Validate(ComponentContext context)
    {
        foreach (var node in RootNodes) node.Validate(context);
    }

    public string Render(ComponentContext context)
        => string.Join(",\n", RootNodes.Select(x => x.Render(context)));

    public sealed class Node
    {
        public ComponentNode Inner { get; }
        public ComponentState State => _state;
        public List<Node> Children { get; }
        public List<Node> AttributeNodes { get; }
        public Node? Parent { get; }

        public IReadOnlyList<Diagnostic> Diagnostics
            =>
            [
                .._diagnostics,
                ..AttributeNodes.SelectMany(x => x.Diagnostics),
                ..Children.SelectMany(x => x.Diagnostics)
            ];

        public bool Incremental { get; }

        private readonly List<Diagnostic> _diagnostics;
        private string? _render;

        private ComponentState _state;

        public Node(
            ComponentNode inner,
            ComponentState state,
            List<Node> children,
            List<Node> attributeNodes,
            Node? parent = null,
            IReadOnlyList<Diagnostic>? diagnostics = null,
            bool incremental = false,
            string? render = null
        )
        {
            Inner = inner;
            _state = state;
            Children = children;
            AttributeNodes = attributeNodes;
            Parent = parent;
            _diagnostics = [..diagnostics ?? []];
            _render = render;
            Incremental = incremental;
        }

        public void UpdateState(ComponentContext context)
        {
            foreach (var attributeNode in AttributeNodes)
                attributeNode.UpdateState(context);

            foreach (var node in Children)
                node.UpdateState(context);

            Inner.UpdateState(ref _state, context);
        }

        public string Render(IComponentContext context, ComponentRenderingOptions options = default)
        {
            if (_render is not null) return _render;

            using (context.CreateDiagnosticScope(_diagnostics))
            {
                return _render = Inner.Render(State, context, options);
            }
        }

        public void Validate(IComponentContext context)
        {
            using (context.CreateDiagnosticScope(_diagnostics))
            {
                Inner.Validate(State, context);
                foreach (var attributeNode in AttributeNodes) attributeNode.Validate(context);
                foreach (var child in Children) child.Validate(context);
            }
        }

        public Node Reuse(Node? parent, IncrementalParseResult parseResult, CXGraphManager manager)
        {
            var diagnostics = new List<Diagnostic>(Diagnostics);

            if (diagnostics.Count > 0)
            {
                var offset = 0;
                var changeQueue = new Queue<TextChange>(parseResult.Changes);
                for (var i = 0; i < diagnostics.Count; i++)
                {
                    var diagnostic = diagnostics[i];
                    var diagnosticSpan = diagnostic.Location.SourceSpan;

                    while (changeQueue.Count > 0)
                    {
                        TextChangeRange change = changeQueue.Peek();

                        if (change.Span.Start < diagnosticSpan.Start)
                        {
                            offset += (change.NewLength - change.Span.Length);
                            changeQueue.Dequeue();
                        }
                    }

                    if (offset is not 0)
                    {
                        // we love roslyn making 'WithLocation' internal *smile*
                        diagnostics[i] = Diagnostic.Create(
                            diagnostic.Descriptor.Id,
                            diagnostic.Descriptor.Category,
                            diagnostic.GetMessage(),
                            diagnostic.Severity,
                            diagnostic.Descriptor.DefaultSeverity,
                            diagnostic.Descriptor.IsEnabledByDefault,
                            diagnostic.WarningLevel,
                            diagnostic.Descriptor.Title,
                            diagnostic.Descriptor.Description,
                            diagnostic.Descriptor.HelpLinkUri,
                            GetLocation(
                                manager,
                                new TextSpan(diagnosticSpan.Start + offset, diagnosticSpan.Length)
                            ),
                            null,
                            diagnostic.Descriptor.CustomTags,
                            diagnostic.Properties
                        );
                    }
                }
            }

            return new(
                Inner,
                State,
                Children,
                AttributeNodes,
                parent,
                diagnostics,
                true,
                _render
            );
        }
    }
}