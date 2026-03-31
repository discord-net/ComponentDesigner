using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using ComponentDesigner.Nodes;
using ComponentDesigner.Parser;
using Priority_Queue;

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
        GetUpdateFlags(this, parameters, out var flags);

        if (flags.HasFlag(UpdateFlags.Syntax))
        {
            // TODO: incremental update from syntax, could diff syntax nodes
            return Create(
                parameters,
                cancellationToken
            );
        }

        if (
            flags.HasFlag(UpdateFlags.Interpolations)
        )
        {
            using var updater = new GraphUpdater(
                this,
                parameters.CompilationProvider,
                UpdateMode.Interpolations,
                cx: parameters.CX,
                options: parameters.Options
            );
            return updater.Run(cancellationToken);
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
        return this;

        // using var updater = new GraphUpdater(this, compilationProvider);
        // return updater.Run(cancellationToken);
    }

    [Flags]
    private enum UpdateMode : byte
    {
        Interpolations = 1 << 0,
        Compilation = 1 << 1,

        All = byte.MaxValue
    }

    private sealed class GraphUpdater : IComponentContext, IDisposable
    {
        public CXComponentGraph OldGraph { get; }

        public IComponentImplementation Implementation => OldGraph.Implementation;

        public ICompilationProvider CompilationProvider { get; }

        public ICXModel CX => _cx ?? OldGraph.CX;

        public IGraphOptions Options => _options ?? OldGraph.Options;

        private ComponentState?[] _newStates;
        private SimplePriorityQueue<GraphNode> _queue;
        private SimplePriorityQueue<GraphNode> _nodesToRecreate;
        private bool _hasUpdatedState;
        private PooledDiagnosticBag _diagnosticsBag;
        private bool _disposed;

        private readonly UpdateMode _mode;
        private readonly ICXModel? _cx;
        private readonly IGraphOptions? _options;

        private readonly GraphInitializationContext _initializationContext;

        public GraphUpdater(
            CXComponentGraph oldGraph,
            ICompilationProvider compilationProvider,
            UpdateMode mode = UpdateMode.All,
            ICXModel? cx = null,
            IGraphOptions? options = null
        )
        {
            _mode = mode;
            _cx = cx;
            _options = options;
            OldGraph = oldGraph;
            CompilationProvider = compilationProvider;

            _newStates = ArrayPool<ComponentState?>.Shared.Rent(OldGraph._tree.Count);
            _queue = new();
            _nodesToRecreate = new();
            _diagnosticsBag = PooledDiagnosticBag.Get();

            _initializationContext = new(
                oldGraph.Document,
                cx ?? oldGraph.CX,
                options ?? oldGraph.Options,
                oldGraph.Implementation,
                compilationProvider,
                _diagnosticsBag,
                OldGraph._tree.Clone()
            );
        }

        public CXComponentGraph Run(CancellationToken cancellationToken)
        {
            if (!OldGraph._tree.HasExternalDependencies) return OldGraph;

            foreach (var node in OldGraph._tree.NodesWithExternalDependencies)
            {
                if (ShouldSkip(node.Component, _mode)) continue;

                _queue.Enqueue(node, NodeIdToPriority(node));
            }

            while (_queue.TryDequeue(out var graphNode))
            {
                if (_nodesToRecreate.TryRemove(graphNode))
                {
                    RecreateNode(graphNode, cancellationToken);
                    continue;
                }

                var newState = graphNode.Component.UpdateState(
                    graphNode.State,
                    this,
                    _diagnosticsBag,
                    cancellationToken
                );

                if (newState is not null)
                {
                    _newStates[graphNode.Id] = newState;
                    _hasUpdatedState |= !newState.Equals(graphNode.State);
                    continue;
                }

                // the nodes new state is null, meaning we have to re-create the parent
                if (graphNode.Parent is not null)
                {
                    if (!_nodesToRecreate.Contains(graphNode.Parent))
                        _nodesToRecreate.Enqueue(graphNode.Parent, NodeIdToPriority(graphNode.Parent));
                }
                else
                {
                    // TODO: root node needs to be recreated
                    throw new NotImplementedException();
                }
            }

            while (_nodesToRecreate.TryDequeue(out var graphNode))
                RecreateNode(graphNode, cancellationToken);

            if (!_hasUpdatedState) return OldGraph;

            throw new NotImplementedException();
        }

        private static bool ShouldSkip(IComponentNode node, UpdateMode mode)
            => node switch
            {
                InterpolationComponentNode => !mode.HasFlag(UpdateMode.Interpolations),
                IDynamicComponentNode { HasExternalDependencies: true } => !mode.HasFlag(UpdateMode.Compilation),
                _ => true
            };

        private void RecreateNode(GraphNode graphNode, CancellationToken cancellationToken)
        {
            // var newNode = new GraphNode(
            //     _initializationContext.Tree,
            //     graphNode.Id,
            //     graphNode.Component,
            //     parentId: graphNode.ParentId
            // );

            graphNode.Component.RegisterGraphNode(
                new(
                    graphNode.Parent,
                    graphNode.State.CXNode,
                    this
                ),
                cancellationToken
            );

            // var context = new ComponentNodeInitializationContext(
            //     graphNode.State.CXNode,
            //     graphNode,
            //     this
            // );
            //
            // var newState = graphNode.Component.Initialize(
            //     context,
            //     _diagnosticsBag,
            //     cancellationToken
            // );
            //
            // if (newState is null) return;
            //
            // _newStates[graphNode.Id] = newState;
            // _hasUpdatedState |= !newState.Equals(graphNode.State);
        }

        private static int NodeIdToPriority(GraphNode graphNode)
            => ~graphNode.Id;

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _diagnosticsBag.Dispose();
                ArrayPool<ComponentState?>.Shared.Return(_newStates);
            }
        }

        public override bool Equals(object? obj)
            => ReferenceEquals(this, obj);

        bool IEquatable<IComponentContext>.Equals(IComponentContext other)
            => Equals(this);
    }

    private sealed class IncrementalUpdater : IGraphContext
    {
        public IComponentImplementation Implementation => _graphUpdater.Implementation;
        public ICompilationProvider CompilationProvider => _graphUpdater.CompilationProvider;
        public ICXModel CX => _graphUpdater.CX;
        public IGraphOptions Options => _graphUpdater.Options;

        private readonly GraphUpdater _graphUpdater;

        public IncrementalUpdater(
            GraphUpdater graphUpdater
        )
        {
            _graphUpdater = graphUpdater;
        }

        public GraphNode? Push(
            GraphNodeInitializationRequest request,
            CancellationToken cancellationToken = default
        )
        {
            throw new NotImplementedException();
        }

        public IReadOnlyList<GraphNode> Push(
            GraphNode? parent,
            IReadOnlyList<ICXNode> syntaxNodes,
            CancellationToken cancellationToken
        )
        {
            throw new NotImplementedException();
        }

        public override bool Equals(object? obj)
            => ReferenceEquals(this, obj);

        public bool Equals(IComponentContext obj)
            => ReferenceEquals(this, obj);
    }
}