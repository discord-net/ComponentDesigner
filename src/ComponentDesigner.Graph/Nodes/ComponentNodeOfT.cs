using ComponentDesigner.Parser;

namespace ComponentDesigner.Nodes;

public delegate void ComponentValidator<in TSelf, in TState>(
    IComponentContext context,
    TSelf self,
    TState state,
    IDiagnosticBag bag
) where TSelf : IComponentNode where TState : ComponentState;

public delegate Result<RenderedComponent> ComponentRenderer<in TSelf, in TState>(
    IRendererContext context,
    TSelf self,
    TState state,
    RendererTypingContext? typingContext,
    CancellationToken cancellationToken
) where TSelf : IComponentNode where TState : ComponentState;

public abstract class ComponentNode<TState> :
    IComponentNode,
    IEquatable<ComponentNode<TState>>
    where TState : ComponentState
{
    public abstract string Name { get; }

    public virtual IReadOnlyList<string> Aliases => [];

    public virtual IReadOnlyList<ComponentProperty> Properties { get; } = [];

    public virtual bool IsParentOfOtherComponents => false;

    public virtual bool IsUserAccessible => true;

    public virtual bool AllowChildrenInCX => IsParentOfOtherComponents;

    public virtual bool HasExternalDependencies => false;

    public abstract TState? Initialize(
        ComponentNodeInitializationContext context,
        IDiagnosticBag diagnostics,
        CancellationToken cancellationToken = default
    );

    public virtual TState UpdateState(
        TState state,
        IComponentContext context,
        IDiagnosticBag diagnostics,
        CancellationToken cancellationToken = default) => state;

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
            parent: context.ParentGraphNode
        );
    }

    public abstract Result<RenderedComponent> Emit(
        TState state,
        ComponentEmitContext context,
        ComponentOptions options,
        CancellationToken cancellationToken = default
    );

    protected Result<RenderedComponent> ValidateAndRender<TSelf>(
        TSelf self,
        TState state,
        ComponentEmitContext context,
        ComponentOptions options,
        ComponentValidator<TSelf, TState> validator,
        ComponentRenderer<TSelf, TState> renderer,
        CancellationToken cancellationToken = default
    ) where TSelf : ComponentNode<TState>
    {
        using var bag = PooledDiagnosticBag.Get();

        validator(context, self, state, bag);

        if (bag.HasErrors) return new(bag.ToCollection());

        var result = renderer(context, self, state, options.TypingContext, cancellationToken)
            .AddDiagnostics(bag);

        if (context.ComponentTypingProvider is null)
            return result;

        return result.Map(render =>
        {
            if (options.TypingContext?.ConformingType is null || render.Type is null)
            {
                // TODO: error?
                return result;
            }

            return context
                .ComponentTypingProvider
                .Convert(
                    context,
                    render.Source.SourcedAt(state.TextSpan),
                    render.Type,
                    options.TypingContext.Value.ConformingType,
                    cancellationToken
                )
                .Map(converted => new RenderedComponent(converted, options.TypingContext.Value.ConformingType));
        });
    }

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

    ComponentState? IComponentNode.Initialize(
        ComponentNodeInitializationContext context,
        IDiagnosticBag diagnostics,
        CancellationToken cancellationToken
    ) => Initialize(context, diagnostics, cancellationToken);

    ComponentState IComponentNode.UpdateState(ComponentState state,
        IComponentContext context,
        IDiagnosticBag diagnostics,
        CancellationToken cancellationToken) => UpdateState((TState)state, context, diagnostics, cancellationToken);

    Result<RenderedComponent> IComponentNode.Emit(
        ComponentState state,
        ComponentEmitContext context,
        ComponentOptions options,
        CancellationToken cancellationToken
    ) => Emit((TState)state, context, options, cancellationToken);
}