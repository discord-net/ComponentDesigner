using System.Diagnostics.CodeAnalysis;
using ComponentDesigner.Parser;

namespace ComponentDesigner.Nodes;

public abstract class ComponentNode<TState> :
    IComponentNode,
    IEquatable<ComponentNode<TState>>
    where TState : ComponentState
{
    public abstract string Name { get; }

    public virtual IReadOnlyList<string> Aliases => [];

    public virtual IReadOnlyList<ComponentProperty> Properties { get; } = [];

    public virtual ComponentTargetType Target => ComponentTargetType.Any;
    
    public virtual bool IsUserAccessible => true;
    
    public virtual bool HasExternalDependencies => false;
    
    public abstract TState? CreateState(
        ComponentNodeInitializationContext context,
        IDiagnosticBag diagnostics,
        CancellationToken cancellationToken = default
    );

    public virtual TState? UpdateState(
        TState state,
        IComponentContext context,
        IDiagnosticBag diagnostics,
        CancellationToken cancellationToken = default
    ) => state;

    public virtual void RegisterGraphNode(
        ComponentGraphInitializationContext context,
        CancellationToken cancellationToken = default
    ) => RegisterGraphNode(context, includeElementChildren: true, cancellationToken);

    protected void RegisterGraphNode(
        ComponentGraphInitializationContext context,
        bool includeElementChildren = true,
        CancellationToken cancellationToken = default
    )
    {
        context.Push(
            this,
            cxNode: context.CXNode,
            children: context.CXNode is CXElement element && includeElementChildren
                ? element.Children
                : null,
            parent: context.ParentGraphNode,
            cancellationToken: cancellationToken
        );
    }

    public virtual void Validate(
        IComponentContext context,
        TState state,
        IDiagnosticBag bag,
        CancellationToken cancellationToken = default
    ) => Validators.ValidateGenericComponent(context, this, state, bag);

    public virtual Result<TRender> Render<TRender>(
        IRenderContext<TRender> context,
        TState state,
        CancellationToken cancellationToken = default
    ) => context.Renderer.RenderComponent(context, this, state, cancellationToken);

    public bool Equals(ComponentNode<TState>? other)
        => ReferenceEquals(this, other);

    public bool Equals(IComponentNode? other)
        => other is ComponentNode<TState> comp && Equals(comp);

    public override bool Equals(object? obj)
        => obj is ComponentNode<TState> other && Equals(other);

    public override int GetHashCode()
    {
        // use the default hash function
        // ReSharper disable once BaseObjectGetHashCodeCallInGetHashCode
        return base.GetHashCode();
    }

    ComponentState? IComponentNode.CreateState(
        ComponentNodeInitializationContext context,
        IDiagnosticBag diagnostics,
        CancellationToken cancellationToken
    ) => CreateState(context, diagnostics, cancellationToken);

    ComponentState? IComponentNode.UpdateState(
        ComponentState state,
        IComponentContext context,
        IDiagnosticBag diagnostics,
        CancellationToken cancellationToken
    ) => UpdateState((TState)state, context, diagnostics, cancellationToken);

    void IComponentNode.Validate(
        IComponentContext context, ComponentState state, IDiagnosticBag bag,
        CancellationToken cancellationToken
    )
    {
        if (state is TState typedState) Validate(context, typedState, bag, cancellationToken);
    }

    Result<TRender> IComponentNode.Render<TRender>(
        IRenderContext<TRender> context,
        ComponentState state,
        CancellationToken cancellationToken
    )
    {
        if (state is TState typedState) 
            return Render(context, typedState, cancellationToken);

        return Diagnostic.StateTypeMismatch(typeof(TState), state).At(state);
    }
}