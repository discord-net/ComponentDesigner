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
    /// A <see cref="CapturingGraphContext"/> is used during
    /// <see cref="IComponentNode.Initialize"/> to intercept all child-node
    /// creation requests (<see cref="IGraphContext.Push(GraphNodeInitializationRequest, CancellationToken)"/>
    /// and <see cref="IGraphContext.Push(GraphNode?, IReadOnlyList{ICXNode}, CancellationToken)"/>)
    /// without mutating the tree. The captured requests are then compared
    /// against the parent's existing children so that unchanged subtrees can
    /// be reused and only the differing children are rebuilt.
    /// </para>
    /// </summary>
    private static void RebuildParentSubtree(
        GraphNode parentNode,
        GraphInitializationContext context,
        CancellationToken cancellationToken
    )
    {
        // ── Step 1: Capture what Initialize *would* produce ──────────
        // We feed a CapturingGraphContext to Initialize so that every call
        // to Push / PushAsChildren is recorded instead of applied.
        var capturingContext = new CapturingGraphContext(context, parentNode);

        var captureInitContext = new ComponentNodeInitializationContext(
            parentNode.State.CXNode,
            parentNode,
            capturingContext
        );

        using var captureDiagnostics = PooledDiagnosticBag.Get();

        // Run Initialize — this calls the component's Initialize which may
        // push children, set properties, etc.  All graph mutations are
        // captured instead of applied.
        var capturedState = parentNode.Component.Initialize(
            captureInitContext,
            captureDiagnostics,
            cancellationToken
        );

        if (capturedState is null)
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

        // ── Step 2: Diff captured children against existing children ─
        var existingChildren = parentNode.HasChildren
            ? parentNode.Children.ToArray()
            : [];

        var capturedChildren = capturingContext.CapturedChildren;

        // Build a lookup of existing children keyed by (ComponentType, CXNode)
        // so that we can efficiently find reusable nodes.
        var existingByIdentity = new Dictionary<(Type, ICXNode?), List<GraphNode>>();
        foreach (var existing in existingChildren)
        {
            var key = (existing.Component.GetType(), existing.State.CXNode);
            if (!existingByIdentity.TryGetValue(key, out var list))
                existingByIdentity[key] = list = [];
            list.Add(existing);
        }

        // Track which existing children we reused so we can dereference the rest.
        var reused = new HashSet<int>();

        // ── Step 3: Process each captured child request ──────────────
        // For each child that Initialize wants, check if an equivalent
        // existing child can be reused. If not, create it fresh.
        foreach (var captured in capturedChildren)
        {
            switch (captured)
            {
                case CapturedChild.FromRequest fromRequest:
                {
                    var req = fromRequest.Request;
                    var childKey = (req.Component.GetType(), req.CXNode);

                    if (
                        existingByIdentity.TryGetValue(childKey, out var candidates) &&
                        candidates.Count > 0
                    )
                    {
                        // Reuse the first matching existing child.
                        var match = candidates[0];
                        candidates.RemoveAt(0);
                        reused.Add(match.Id);
                        // The node is already in the tree; no action needed.
                    }
                    else
                    {
                        // New child — create it in the real tree.
                        var newReq = req with { Parent = parentNode };
                        context.Push(newReq, cancellationToken);
                    }

                    break;
                }

                case CapturedChild.FromSyntaxNodes fromSyntax:
                {
                    // Children pushed via PushAsChildren / AddChild.
                    // Try to reuse existing children that match each syntax node.
                    foreach (var syntaxNode in fromSyntax.SyntaxNodes)
                    {
                        var found = false;

                        foreach (var existing in existingChildren)
                        {
                            if (reused.Contains(existing.Id)) continue;

                            if (
                                existing.State.CXNode is not null &&
                                ReferenceEquals(existing.State.CXNode, syntaxNode)
                            )
                            {
                                reused.Add(existing.Id);
                                found = true;
                                break;
                            }
                        }

                        if (!found)
                        {
                            // New child from syntax — create it.
                            CreateNodes(syntaxNode, parentNode, context, cancellationToken);
                        }
                    }

                    break;
                }
            }
        }

        // ── Step 4: Dereference children that were not reused ────────
        foreach (var existing in existingChildren)
        {
            if (!reused.Contains(existing.Id))
                context.Tree.DereferenceSubtree(existing);
        }

        // ── Step 5: Apply the new state ──────────────────────────────
        parentNode.State = capturedState;
        parentNode.ComponentInitializationProducedDiagnostics = captureDiagnostics.HasAny;

        if (captureDiagnostics.HasAny)
            context.Diagnostics.Add(captureDiagnostics.ToCollection());
    }

    /// <summary>
    /// Represents a child-node creation captured by
    /// <see cref="CapturingGraphContext"/> during
    /// <see cref="IComponentNode.Initialize"/>.
    /// </summary>
    private abstract record CapturedChild
    {
        /// <summary>A child requested via <see cref="IGraphContext.Push(GraphNodeInitializationRequest, CancellationToken)"/>.</summary>
        public sealed record FromRequest(GraphNodeInitializationRequest Request) : CapturedChild;

        /// <summary>Children requested via <see cref="IGraphContext.Push(GraphNode?, IReadOnlyList{ICXNode}, CancellationToken)"/>.</summary>
        public sealed record FromSyntaxNodes(IReadOnlyList<ICXNode> SyntaxNodes) : CapturedChild;
    }

    /// <summary>
    /// A lightweight <see cref="IGraphContext"/> that records all
    /// child-creation requests made during
    /// <see cref="IComponentNode.Initialize"/> without modifying the tree.
    /// This enables <see cref="RebuildParentSubtree"/> to compare the
    /// component's desired child structure against the existing children
    /// and reuse unchanged subtrees.
    /// </summary>
    private sealed class CapturingGraphContext : IGraphContext
    {
        private readonly IComponentContext _source;
        private readonly GraphNode _parentNode;
        private readonly List<CapturedChild> _capturedChildren = [];

        public CapturingGraphContext(IComponentContext source, GraphNode parentNode)
        {
            _source = source;
            _parentNode = parentNode;
        }

        public IComponentImplementation Implementation => _source.Implementation;
        public ICompilationProvider CompilationProvider => _source.CompilationProvider;
        public ICXModel CX => _source.CX;
        public IGraphOptions Options => _source.Options;

        /// <summary>All captured child-creation requests, in order.</summary>
        public IReadOnlyList<CapturedChild> CapturedChildren => _capturedChildren;

        public GraphNode? Push(
            GraphNodeInitializationRequest request,
            CancellationToken cancellationToken = default
        )
        {
            _capturedChildren.Add(new CapturedChild.FromRequest(request));

            // Return an existing child that matches, if any, so that
            // Initialize code that references the returned node (e.g. to set
            // property values) keeps working during the capture pass.
            if (_parentNode.HasChildren)
            {
                foreach (var existing in _parentNode.Children)
                {
                    if (
                        existing.Component.GetType() == request.Component.GetType() &&
                        ReferenceEquals(existing.State.CXNode, request.CXNode)
                    )
                    {
                        return existing;
                    }
                }
            }

            return null;
        }

        public IReadOnlyList<GraphNode> Push(
            GraphNode? parent,
            IReadOnlyList<ICXNode> syntaxNodes,
            CancellationToken cancellationToken
        )
        {
            _capturedChildren.Add(new CapturedChild.FromSyntaxNodes(syntaxNodes));

            // Return existing children that match, so Initialize code
            // referencing returned nodes keeps working.
            if (_parentNode.HasChildren)
            {
                var result = new List<GraphNode>();

                foreach (var syntaxNode in syntaxNodes)
                {
                    foreach (var existing in _parentNode.Children)
                    {
                        if (
                            existing.State.CXNode is not null &&
                            ReferenceEquals(existing.State.CXNode, syntaxNode)
                        )
                        {
                            result.Add(existing);
                            break;
                        }
                    }
                }

                return result;
            }

            return [];
        }

        public bool Equals(IComponentContext? other)
            => other is CapturingGraphContext ctx
               && ReferenceEquals(_source, ctx._source);
    }
}