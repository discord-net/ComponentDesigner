using ComponentDesigner.Nodes;
using ComponentDesigner.Parser;

namespace ComponentDesigner;

partial class CXComponentGraph
{
    [Flags]
    private enum UpdateFlags
    {
        None = 0,
        Syntax = 1 << 0,
        Interpolations = 1 << 1,
        Options = 1 << 2,
    }

    public CXComponentGraph UpdateFromParameters(
        GraphParameters parameters,
        CancellationToken cancellationToken
    )
    {
        // Decide whether we can incrementally update from the current graph snapshot.
        GetUpdateFlags(this, parameters, out var flags);

        if (flags is UpdateFlags.None)
            return this;

        if (flags.HasFlag(UpdateFlags.Syntax))
        {
            // Syntax changes currently require a full graph reconstruction.
            return Create(
                parameters,
                cancellationToken
            );
        }

        if (
            flags.HasFlag(UpdateFlags.Interpolations)
        )
        {
            // Interpolation metadata changed; run incremental update path that may
            // update states and rebuild local parent scopes as needed.
            return Update(
                UpdateMode.Interpolations,
                parameters.CompilationProvider,
                parameters.CX,
                parameters.Options,
                cancellationToken
            );
        }

        if (flags.HasFlag(UpdateFlags.Options))
        {
            // Option changes can affect node shape/validation globally.
            return Update(
                UpdateMode.All,
                parameters.CompilationProvider,
                parameters.CX,
                parameters.Options,
                cancellationToken
            );
        }

        return this;

        static void GetUpdateFlags(
            CXComponentGraph graph,
            GraphParameters parameters,
            out UpdateFlags flags
        )
        {
            flags = UpdateFlags.None;

            if (!graph.CX.Syntax.Equals(parameters.CX.Syntax))
                flags |= UpdateFlags.Syntax;

            if (
                !graph.CX.Interpolations.SequenceEqual(parameters.CX.Interpolations) ||
                graph.CX.DesignerParameterName != parameters.CX.DesignerParameterName ||
                graph.CX.UsesDesignerParameter != parameters.CX.UsesDesignerParameter
            ) flags |= UpdateFlags.Interpolations;

            if (!graph.Options.Equals(parameters.Options))
                flags |= UpdateFlags.Options;
        }
    }

    public CXComponentGraph UpdateFromCompilation(
        ICompilationProvider compilationProvider,
        CancellationToken cancellationToken
    )
    {
        return Update(
            UpdateMode.Compilation,
            compilationProvider,
            CX,
            Options,
            cancellationToken
        );
    }

    [Flags]
    private enum UpdateMode : byte
    {
        None = 0,
        
        Compilation = 1 << 0,
        Interpolations = 1 << 1,
        
        All = byte.MaxValue
    }

    private CXComponentGraph Update(
        UpdateMode mode,
        ICompilationProvider compilationProvider,
        ICXModel cx,
        IGraphOptions options,
        CancellationToken cancellationToken
    )
    {
        using var diagnostics = PooledDiagnosticBag.Get();
        var updateContext = new GraphUpdateContext(cx, options, Implementation, compilationProvider);

        // ── Phase 1: Evaluate state updates ──────────────────────────────
        // Walk every node that declared external dependencies and, depending
        // on the requested mode, ask the component to produce an updated
        // state.  Results fall into one of three buckets:
        //
        //   • Same state (Case 1)   – nothing to do.
        //   • New state   (Case 2)  – update the node in-place.
        //   • Null         (Case 3) – node is invalid in the new context;
        //                             the parent subtree must be rebuilt.
        Dictionary<int, ComponentState>? stateUpdates = null;
        HashSet<int>? parentsToRebuild = null;
        HashSet<int>? rootsToRebuild = null;

        foreach (var node in _tree.NodesWithExternalDependencies)
        {
            if (!ShouldUpdate(node, mode))
                continue;

            var newState = node.Component.UpdateState(
                node.State, updateContext, diagnostics, cancellationToken
            );

            if (newState is null)
            {
                // Case 3: node is no longer valid.  Mark its parent (or
                // itself if it is a root) for rebuilding.
                if (node.ParentId.HasValue)
                    (parentsToRebuild ??= []).Add(node.ParentId.Value);
                else
                    (rootsToRebuild ??= []).Add(node.Id);
            }
            else if (!newState.Equals(node.State))
            {
                // Case 2: state changed.
                (stateUpdates ??= []).Add(node.Id, newState);
            }
            // Case 1: state unchanged – no action required.
        }

        // Fast-path: nothing changed → reuse this graph as-is.
        if (stateUpdates is null && parentsToRebuild is null && rootsToRebuild is null)
            return this;

        // ── Phase 2: Clone tree & apply in-place state updates ───────────
        var newTree = _tree.Clone();

        if (stateUpdates is not null)
        {
            foreach (var entry in stateUpdates)
                newTree[entry.Key].State = entry.Value;
        }

        // ── Phase 3: Rebuild subtrees for invalidated nodes ──────────────
        if (parentsToRebuild is not null || rootsToRebuild is not null)
        {
            var initContext = new GraphInitializationContext(
                Document, cx, options,
                Implementation, compilationProvider,
                diagnostics, newTree
            );

            // Rebuild parent subtrees whose children were invalidated.
            if (parentsToRebuild is not null)
            {
                foreach (var parentId in parentsToRebuild)
                    RebuildParentSubtree(newTree[parentId], initContext, cancellationToken);
            }

            // Re-create root-level nodes that returned null.
            if (rootsToRebuild is not null)
            {
                foreach (var rootId in rootsToRebuild)
                {
                    var rootNode = newTree[rootId];
                    var cxNode = rootNode.State.CXNode;
                    newTree.DereferenceSubtree(rootNode);

                    if (cxNode is not null)
                        CreateNodes(cxNode, null, initContext, cancellationToken);
                }
            }
        }

        return new CXComponentGraph(
            Document,
            newTree,
            _diagnostics,
            cx, options,
            Implementation,
            diagnostics.Count > 0 ? diagnostics.ToCollection() : _updateDiagnostics
        );
    }

    /// <summary>
    /// Determines whether <paramref name="node"/> should be updated for the
    /// current <paramref name="mode"/>.  Interpolation nodes are updated only
    /// when <see cref="UpdateMode.Interpolations"/> is set; all other node
    /// types require <see cref="UpdateMode.Compilation"/>.
    /// </summary>
    private static bool ShouldUpdate(GraphNode node, UpdateMode mode)
    {
        if (node.Component is InterpolationComponentNode)
            return mode.HasFlag(UpdateMode.Interpolations);

        return mode.HasFlag(UpdateMode.Compilation);
    }

    /// <summary>
    /// Rebuilds the children and state of <paramref name="parentNode"/>
    /// after one or more of its children were invalidated during an
    /// incremental update.
    /// <para>
    /// The rebuild proceeds in three stages:
    /// <list type="number">
    ///   <item>
    ///     <see cref="IComponentNode.RegisterGraphNode"/> is called through a
    ///     <see cref="CapturingGraphContext"/> to obtain the
    ///     <see cref="GraphNodeInitializationRequest"/> that describes the
    ///     node's desired children syntax without mutating the tree. This
    ///     captures the <c>Children</c> syntax nodes that would normally be
    ///     passed to <see cref="CreateFromInitializationRequest"/>.
    ///   </item>
    ///   <item>
    ///     The captured children syntax is diffed against the parent's
    ///     existing child graph nodes.  Children whose syntax identity
    ///     (<see cref="ICXNode"/> reference) matches an existing child are
    ///     reused; the rest are created fresh and non-reused children are
    ///     dereferenced.
    ///   </item>
    ///   <item>
    ///     <see cref="IComponentNode.Initialize"/> is called with the
    ///     <b>real</b> <see cref="GraphInitializationContext"/> so that the
    ///     new state correctly references the actual (reused or newly created)
    ///     child graph nodes.
    ///   </item>
    /// </list>
    /// </para>
    /// </summary>
    private static void RebuildParentSubtree(
        GraphNode parentNode,
        GraphInitializationContext context,
        CancellationToken cancellationToken
    )
    {
        // ── Step 1: Capture what RegisterGraphNode would produce ─────
        // RegisterGraphNode describes how the node is placed in the graph
        // and, critically, which CX syntax children should be processed.
        // We capture without mutating the tree.
        var capturingContext = new CapturingGraphContext(context);
        var regContext = new ComponentGraphInitializationContext(
            parentNode.Parent,
            parentNode.State.CXNode,
            capturingContext
        );

        parentNode.Component.RegisterGraphNode(regContext, cancellationToken);

        if (capturingContext.CapturedRequests.Count == 0)
            return;

        var request = capturingContext.CapturedRequests[0];

        // ── Step 2: Snapshot existing children before mutation ────────
        var existingChildren = parentNode.HasChildren
            ? parentNode.Children.ToArray()
            : Array.Empty<GraphNode>();

        // ── Step 3: Dereference all existing children ────────────────
        // We remove them from the tree structure first — reused nodes
        // will be re-added when CreateNodes / CreateFromInitializationRequest
        // re-creates them. Unreused ones stay dereferenced.
        context.Tree.DereferenceChildren(parentNode);

        // ── Step 4: Re-create attribute children and syntax children ─
        // Process attribute-embedded elements.
        if (request.CXNode is CXElement { OpeningTag.Attributes: { Count: > 0 } attributes })
        {
            foreach (var attribute in attributes)
            {
                if (attribute.Value is not CXValue.Element nestedElement) continue;
                CreateNodes(nestedElement.Value, parentNode, context, cancellationToken);
            }
        }

        // Process the request's Children syntax nodes. This is where
        // RegisterGraphNode's children list (e.g. element.Children for
        // ContainerComponentNode) gets turned into graph nodes.
        if (request.Children?.Count > 0)
            CreateNodes(request.Children, parentNode, context, cancellationToken);

        // ── Step 5: Call Initialize with the real context ─────────────
        // Initialize runs against the actual tree so the new state
        // correctly references the (potentially newly created) child
        // graph nodes — e.g. SetPropertyValueToChildren will pick up the
        // new children list.
        var initContext = new ComponentNodeInitializationContext(
            request.CXNode,
            parentNode,
            context
        );

        var numDiagnostics = context.Diagnostics.Count;

        var newState = parentNode.Component.Initialize(
            initContext,
            context.Diagnostics,
            cancellationToken
        );

        parentNode.ComponentInitializationProducedDiagnostics =
            numDiagnostics != context.Diagnostics.Count;

        if (newState is null)
        {
            // The parent node itself is no longer valid. Cascade upward.
            if (parentNode.ParentId.HasValue)
            {
                RebuildParentSubtree(
                    context.Tree[parentNode.ParentId.Value],
                    context,
                    cancellationToken
                );
            }
            else
            {
                var cxNode = parentNode.State.CXNode;
                context.Tree.DereferenceSubtree(parentNode);

                if (cxNode is not null)
                    CreateNodes(cxNode, null, context, cancellationToken);
            }

            return;
        }

        parentNode.State = newState;
    }

    /// <summary>
    /// A lightweight <see cref="IGraphContext"/> that records
    /// <see cref="GraphNodeInitializationRequest"/>s captured during
    /// <see cref="IComponentNode.RegisterGraphNode"/> without modifying any
    /// tree. This enables the update path to inspect the component's desired
    /// child structure before committing changes.
    /// </summary>
    private sealed class CapturingGraphContext : IGraphContext
    {
        private readonly IComponentContext _source;
        private readonly List<GraphNodeInitializationRequest> _captured = [];

        public CapturingGraphContext(IComponentContext source)
        {
            _source = source;
        }

        public IComponentImplementation Implementation => _source.Implementation;
        public ICompilationProvider CompilationProvider => _source.CompilationProvider;
        public ICXModel CX => _source.CX;
        public IGraphOptions Options => _source.Options;

        /// <summary>
        /// The initialization requests captured from
        /// <see cref="IComponentNode.RegisterGraphNode"/>.
        /// </summary>
        public IReadOnlyList<GraphNodeInitializationRequest> CapturedRequests => _captured;

        public GraphNode? Push(
            GraphNodeInitializationRequest request,
            CancellationToken cancellationToken = default
        )
        {
            _captured.Add(request);
            return null;
        }

        public IReadOnlyList<GraphNode> Push(
            GraphNode? parent,
            IReadOnlyList<ICXNode> syntaxNodes,
            CancellationToken cancellationToken
        ) => [];

        public bool Equals(IComponentContext? other)
            => other is CapturingGraphContext ctx
               && ReferenceEquals(_source, ctx._source);
    }
}