using ComponentDesigner.Parser;

namespace ComponentDesigner.Nodes;

public abstract class ComponentNode<T> :
    IComponentNode,
    IEquatable<ComponentNode<T>>
    where T : ComponentState
{
    public abstract string Name { get; }

    public virtual IReadOnlyList<string> Aliases => [];

    public virtual IReadOnlyList<ComponentProperty> Properties { get; } = [];

    public virtual bool IsParentOfOtherComponents => false;

    public virtual bool IsUserAccessible => true;

    public virtual bool AllowChildrenInCX => IsParentOfOtherComponents;

    public abstract T? Initialize(
        ComponentNodeInitializationContext context,
        IDiagnosticBag diagnostics,
        CancellationToken cancellationToken = default
    );

    public virtual T UpdateState(
        T state,
        IGraphContext context,
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
                : null
        );
    }

    public abstract Result<RenderedComponent> Emit(
        T state,
        ComponentEmitContext context,
        ComponentOptions options,
        CancellationToken cancellationToken = default
    );

    protected Result<RenderedComponent> ValidateAndRender<TSelf>(
        TSelf self,
        T state,
        ComponentEmitContext context,
        ComponentOptions options,
        Action<IComponentContext, TSelf, T, IDiagnosticBag> validator,
        Func<IRendererContext, TSelf, T, RendererTypingContext?, CancellationToken, Result<RenderedComponent>> renderer,
        CancellationToken cancellationToken = default
    ) where TSelf : ComponentNode<T>
    {
        using var bag = PooledDiagnosticBag.Get();

        validator(context, self, state, bag);

        if (bag.HasErrors) return new(bag.ToCollection());

        return renderer(context, self, state, options.TypingContext, cancellationToken).AddDiagnostics(bag);
    }

    public bool Equals(ComponentNode<T>? other)
        => ReferenceEquals(this, other);

    public bool Equals(IComponentNode? other)
        => other is ComponentNode<T> comp && Equals(comp);

    public override bool Equals(object? obj)
        => obj is ComponentNode<T> other && Equals(other);

    public override int GetHashCode()
    {
        // use the default hash function
        // ReSharper disable once BaseObjectGetHashCodeCallInGetHashCode
        return base.GetHashCode();
    }

    ComponentState? IComponentNode.Initialize(
        ComponentNodeInitializationContext context,
        IDiagnosticBag diagnostics,
        CancellationToken cancellationToken
    ) => Initialize(context, diagnostics, cancellationToken);

    ComponentState IComponentNode.UpdateState(
        ComponentState state,
        IGraphContext context,
        IDiagnosticBag diagnostics,
        CancellationToken cancellationToken
    ) => UpdateState((T)state, context, diagnostics, cancellationToken);

    Result<RenderedComponent> IComponentNode.Emit(
        ComponentState state,
        ComponentEmitContext context,
        ComponentOptions options,
        CancellationToken cancellationToken
    ) => Emit((T)state, context, options, cancellationToken);
}