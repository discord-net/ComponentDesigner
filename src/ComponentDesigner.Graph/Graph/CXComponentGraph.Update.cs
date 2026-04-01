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

    public CXComponentGraph Update(
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

    public CXComponentGraph UpdateDependencies(
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

    private CXComponentGraph Update(
        UpdateMode mode,
        ICompilationProvider compilationProvider,
        ICXModel cx,
        IGraphOptions options,
        CancellationToken cancellationToken
    )
    {
        if (mode is 0) return this;

        var includeInterpolationNodes = mode.HasFlag(UpdateMode.Interpolations);
        var includeNonInterpolationNodes = mode.HasFlag(UpdateMode.Compilation);
        var hasExternalNodeUpdates = includeInterpolationNodes || includeNonInterpolationNodes;

        if (hasExternalNodeUpdates && !_tree.HasExternalDependencies)
        {
            // Nothing in the current tree depends on external symbols.
            return this;
        }

        if (hasExternalNodeUpdates)
        {
            var updateContext = new GraphUpdateContext(
                cx,
                options,
                Implementation,
                compilationProvider
            );

            using var updateDiagnostics = PooledDiagnosticBag.Get();
            var hasChanges = false;

            var plans = new Dictionary<int, NodePlan>(capacity: _tree.Count);

            foreach (var oldNode in _tree.NodesWithExternalDependencies)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var isInterpolationNode = ReferenceEquals(oldNode.Component, InterpolationComponentNode.Instance);

                // Interpolation mode: only interpolation nodes
                // Compilation mode: all external nodes except interpolation nodes
                // Combined mode: all external nodes
                if (
                    (isInterpolationNode && !includeInterpolationNodes) ||
                    (!isInterpolationNode && !includeNonInterpolationNodes)
                )
                {
                    continue;
                }

                var updatedState = oldNode.Component.UpdateState(
                    oldNode.State,
                    updateContext,
                    updateDiagnostics,
                    cancellationToken
                );

                if (updatedState is null)
                {
                    if (!MarkParentForRebuild(oldNode, plans, ref hasChanges))
                    {
                        // No reconstructable parent exists (for example, null-sourced root),
                        // so incremental repair is not possible.
                        return RebuildFromCurrentDocument();
                    }

                    continue;
                }

                if (!ReferenceEquals(updatedState, oldNode.State) && !updatedState.Equals(oldNode.State))
                {
                    hasChanges = true;

                    if (plans.TryGetValue(oldNode.Id, out var existingPlan))
                    {
                        plans[oldNode.Id] = existingPlan with
                        {
                            State = updatedState
                        };
                    }
                    else
                    {
                        plans[oldNode.Id] = new(updatedState, RebuildChildren: false);
                    }
                }
            }

            var newTree = new CXComponentTree();

            foreach (var root in _tree.RootNodes)
            {
                if (!plans.TryGetValue(root.Id, out var rootPlan))
                {
                    rootPlan = new(root.State, RebuildChildren: false);
                }

                BuildNode(
                    root,
                    rootPlan,
                    parent: null,
                    newTree,
                    plans,
                    updateContext,
                    updateDiagnostics,
                    ref hasChanges,
                    cancellationToken
                );
            }

            if (!hasChanges &&
                ReferenceEquals(cx, CX) &&
                ReferenceEquals(options, Options) &&
                !updateDiagnostics.HasAny)
            {
                return this;
            }

            return new CXComponentGraph(
                Document,
                newTree,
                _diagnostics,
                cx,
                options,
                Implementation,
                updateDiagnostics.HasAny ? updateDiagnostics.ToCollection() : null
            );

            static bool MarkParentForRebuild(
                GraphNode oldNode,
                Dictionary<int, NodePlan> plans,
                ref bool hasChanges
            )
            {
                var current = oldNode.Parent;

                while (current is not null)
                {
                    if (current.State.CXNode is CXElement)
                    {
                        hasChanges = true;

                        if (plans.TryGetValue(current.Id, out var existingPlan))
                        {
                            plans[current.Id] = existingPlan with
                            {
                                RebuildChildren = true
                            };
                        }
                        else
                        {
                            plans[current.Id] = new(current.State, RebuildChildren: true);
                        }

                        return true;
                    }

                    current = current.Parent;
                }

                return false;
            }

            GraphNode BuildNode(
                GraphNode oldNode,
                NodePlan plan,
                GraphNode? parent,
                CXComponentTree targetTree,
                IReadOnlyDictionary<int, NodePlan> plans,
                GraphUpdateContext updateContext,
                IDiagnosticBag diagnostics,
                ref bool hasChanges,
                CancellationToken cancellationToken
            )
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Phase 1: create the target node with a rebound state.
                var node = CreateReboundNode(oldNode, plan.State, parent, targetTree);

                // Fast path: children can be reused/re-evaluated recursively without re-initializing this parent.
                if (!plan.RebuildChildren)
                {
                    BuildChildrenFromExisting(
                        oldNode.Children,
                        node,
                        targetTree,
                        plans,
                        updateContext,
                        diagnostics,
                        ref hasChanges,
                        cancellationToken
                    );

                    return node;
                }

                // Rebuild path: this parent must repush its children from syntax, then reconcile child-by-child.
                if (oldNode.State.CXNode is not CXElement oldElement)
                {
                    return node;
                }

                var rebuiltChildren = RebuildChildrenFromSyntax(
                    oldNode,
                    node,
                    oldElement,
                    updateContext,
                    diagnostics,
                    cancellationToken
                );

                ReconcileChildren(
                    oldNode.Children,
                    rebuiltChildren,
                    node,
                    targetTree,
                    plans,
                    updateContext,
                    diagnostics,
                    ref hasChanges,
                    cancellationToken
                );

                return node;
            }

            static GraphNode CreateReboundNode(
                GraphNode source,
                ComponentState state,
                GraphNode? parent,
                CXComponentTree targetTree
            )
            {
                var node = targetTree.Push(
                    source.Component,
                    parent: parent
                );

                node.Flags = source.Flags;
                node.State = state with
                {
                    GraphNode = node
                };

                return node;
            }

            void BuildChildrenFromExisting(
                IReadOnlyList<GraphNode> oldChildren,
                GraphNode parent,
                CXComponentTree targetTree,
                IReadOnlyDictionary<int, NodePlan> plans,
                GraphUpdateContext updateContext,
                IDiagnosticBag diagnostics,
                ref bool hasChanges,
                CancellationToken cancellationToken
            )
            {
                foreach (var oldChild in oldChildren)
                {
                    BuildNode(
                        oldChild,
                        GetPlan(oldChild, plans),
                        parent,
                        targetTree,
                        plans,
                        updateContext,
                        diagnostics,
                        ref hasChanges,
                        cancellationToken
                    );
                }
            }

            IReadOnlyList<GraphNode> RebuildChildrenFromSyntax(
                GraphNode oldParent,
                GraphNode reboundParent,
                CXElement oldElement,
                GraphUpdateContext updateContext,
                IDiagnosticBag diagnostics,
                CancellationToken cancellationToken
            )
            {
                var scratchTree = new CXComponentTree();
                var scratchContext = new GraphInitializationContext(
                    Document,
                    updateContext.CX,
                    updateContext.Options,
                    updateContext.Implementation,
                    updateContext.CompilationProvider,
                    diagnostics,
                    scratchTree
                );

                var scratchParent = scratchTree.Push(
                    oldParent.Component,
                    parent: null
                );

                scratchParent.Flags = oldParent.Flags;
                scratchParent.State = reboundParent.State with
                {
                    GraphNode = scratchParent
                };

                return scratchContext.Push(
                    scratchParent,
                    oldElement.Children,
                    cancellationToken
                );
            }

            void ReconcileChildren(
                IReadOnlyList<GraphNode> oldChildren,
                IReadOnlyList<GraphNode> rebuiltChildren,
                GraphNode parent,
                CXComponentTree targetTree,
                IReadOnlyDictionary<int, NodePlan> plans,
                GraphUpdateContext updateContext,
                IDiagnosticBag diagnostics,
                ref bool hasChanges,
                CancellationToken cancellationToken
            )
            {
                if (oldChildren.Count != rebuiltChildren.Count)
                {
                    hasChanges = true;
                }

                var sharedCount = Math.Min(oldChildren.Count, rebuiltChildren.Count);

                for (var i = 0; i < sharedCount; i++)
                {
                    var oldChild = oldChildren[i];
                    var rebuiltChild = rebuiltChildren[i];

                    if (AreEquivalentInitializationAndState(oldChild, rebuiltChild))
                    {
                        BuildNode(
                            oldChild,
                            GetPlan(oldChild, plans),
                            parent,
                            targetTree,
                            plans,
                            updateContext,
                            diagnostics,
                            ref hasChanges,
                            cancellationToken
                        );
                    }
                    else
                    {
                        hasChanges = true;
                        CopySubTree(rebuiltChild, parent, targetTree);
                    }
                }

                for (var i = sharedCount; i < rebuiltChildren.Count; i++)
                {
                    hasChanges = true;
                    CopySubTree(rebuiltChildren[i], parent, targetTree);
                }
            }

            static NodePlan GetPlan(
                GraphNode oldNode,
                IReadOnlyDictionary<int, NodePlan> plans
            ) => plans.TryGetValue(oldNode.Id, out var plan)
                ? plan
                : new(oldNode.State, RebuildChildren: false);

            static bool AreEquivalentInitializationAndState(GraphNode oldNode, GraphNode rebuiltNode)
            {
                if (!oldNode.Component.Equals(rebuiltNode.Component))
                    return false;

                if (!oldNode.State.Equals(rebuiltNode.State))
                    return false;

                if (oldNode.State.CXNode is null || rebuiltNode.State.CXNode is null)
                    return oldNode.State.CXNode is null && rebuiltNode.State.CXNode is null;

                return oldNode.State.CXNode.Equals(rebuiltNode.State.CXNode);
            }

            static GraphNode CopySubTree(
                GraphNode source,
                GraphNode? parent,
                CXComponentTree target
            )
            {
                var node = target.Push(
                    source.Component,
                    parent: parent
                );

                node.Flags = source.Flags;
                node.State = source.State with
                {
                    GraphNode = node
                };

                foreach (var child in source.Children)
                {
                    CopySubTree(child, node, target);
                }

                return node;
            }
        }

        // Non-compilation updates currently rebuild from the existing parsed document.
        return RebuildFromCurrentDocument();

        CXComponentGraph RebuildFromCurrentDocument()
            => Create(
                new(
                    Implementation,
                    compilationProvider,
                    cx,
                    options
                ),
                Document,
                cancellationToken
            );

    }

    // Captures per-node incremental decisions for the current update pass.
    private sealed record NodePlan(
        ComponentState State,
        bool RebuildChildren
    );

    [Flags]
    private enum UpdateMode : byte
    {
        Interpolations = 1 << 0,
        Compilation = 1 << 1,

        All = byte.MaxValue
    }
}