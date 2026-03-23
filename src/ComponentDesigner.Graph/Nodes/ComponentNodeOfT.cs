using System.Diagnostics.CodeAnalysis;
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

    private Dictionary<string, ComponentProperty>? _propertyLookupMap;

    public bool TryGetProperty(string name, [MaybeNullWhen(false)] out ComponentProperty property)
    {
        if (_propertyLookupMap is null)
        {
            _propertyLookupMap = Properties
                .SelectMany(x => x.Aliases.Prepend(x.Name).Select(y => (K: y, V: x)))
                .ToDictionary(x => x.K, x => x.V);
        }

        if (_propertyLookupMap.Count is 0)
        {
            property = null;
            return false;
        }

        return _propertyLookupMap.TryGetValue(name, out property);
    }

    public abstract TState? Initialize(
        ComponentNodeInitializationContext context,
        IDiagnosticBag diagnostics,
        CancellationToken cancellationToken = default
    );

    public virtual TState UpdateState(
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
            parent: context.ParentGraphNode
        );
    }

    public virtual void Validate(
        IComponentContext context,
        TState state,
        IDiagnosticBag bag,
        CancellationToken cancellationToken = default
    ) => Validators.ValidateGenericComponent(this, state, bag);

    public abstract Result<RenderedComponent> Render(
        ComponentEmitContext context,
        TState state,
        ComponentOptions options,
        CancellationToken cancellationToken = default
    );

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

    void IComponentNode.Validate(
        IComponentContext context, ComponentState state, IDiagnosticBag bag,
        CancellationToken cancellationToken
    )
    {
        if (state is TState typedState) Validate(context, typedState, bag, cancellationToken);
    }

    Result<RenderedComponent> IComponentNode.Render(
        ComponentEmitContext context, ComponentState state, ComponentOptions options,
        CancellationToken cancellationToken
    )
    {
        if (state is TState typedState) return Render(context, typedState, options, cancellationToken);

        return default;
    }
}