using System.Collections.Generic;

namespace Discord.CX.Nodes;

public abstract class ComponentNode<TState> : ComponentNode
    where TState : ComponentState
{
    public abstract Result<string> Render(TState state, IComponentContext context, ComponentRenderingOptions options);

    public virtual TState UpdateState(TState state, IComponentContext context, IList<DiagnosticInfo> diagnostics)
        => state;

    public sealed override ComponentState UpdateState(
        ComponentState state,
        IComponentContext context,
        IList<DiagnosticInfo> diagnostics
    ) => UpdateState((TState)state, context, diagnostics);

    public abstract TState? CreateState(ComponentStateInitializationContext context, IList<DiagnosticInfo> diagnostics);

    public sealed override ComponentState? Create(
        ComponentStateInitializationContext context,
        IList<DiagnosticInfo> diagnostics
    ) => CreateState(context, diagnostics);

    public sealed override Result<string> Render(
        ComponentState state,
        IComponentContext context,
        ComponentRenderingOptions options
    ) => Render((TState)state, context, options);

    public virtual void Validate(TState state, IComponentContext context, IList<DiagnosticInfo> diagnostics)
    {
        base.Validate(state, context, diagnostics);
    }

    public sealed override void Validate(ComponentState state, IComponentContext context,
        IList<DiagnosticInfo> diagnostics)
        => Validate((TState)state, context, diagnostics);
}