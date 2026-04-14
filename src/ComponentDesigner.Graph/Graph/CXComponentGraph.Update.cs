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

        foreach (var node in _tree.NodesWithExternalDependencies)
        {
            if (!ShouldUpdate(node, mode))
                continue;

            var newState = node.Component.UpdateState(
                node.State, updateContext, diagnostics, cancellationToken
            );

            if (diagnostics.Count > 0 || newState is null || !newState.Equals(node.State))
                return Create(
                    new(
                        Implementation,
                        compilationProvider,
                        cx,
                        options
                    ),
                    cancellationToken
                );
        }

        return this;
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

}