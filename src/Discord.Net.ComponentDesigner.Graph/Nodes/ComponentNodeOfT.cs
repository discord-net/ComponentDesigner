using Discord.CX.Parser;

namespace Discord.CX.Nodes;

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

    protected virtual bool AllowChildrenInCX => IsParentOfOtherComponents;

    public abstract T? Initialize(
        ComponentNodeInitializationContext context,
        IList<Diagnostic> diagnostics,
        CancellationToken token = default
    );

    public virtual void RegisterGraphNode(
        ComponentGraphInitializationContext context,
        CancellationToken token = default
    )
    {
        context.Push(
            this,
            cxNode: context.CXNode,
            children: IsParentOfOtherComponents && context.CXNode is CXElement element
                ? element.Children
                : null
        );
    }

    public abstract Result<string> Emit(
        T state,
        ComponentEmitContext context,
        ComponentOptions options,
        CancellationToken token = default
    );

    protected void Validate(ComponentState state, IDiagnosticBag bag)
    {
        ValidateElementStructure(state, bag);
        ValidateProperties(state, bag);
    }

    protected void ValidateElementStructure(
        ComponentState state,
        IDiagnosticBag bag,
        bool? allowsChildrenInCXOverride = null,
        bool? isParentOverride = null
    )
    {
        if (state.CXNode is not CXElement element) return;

        var allowsChildrenInCX = allowsChildrenInCXOverride ?? AllowChildrenInCX;
        var isParent = isParentOverride ?? IsParentOfOtherComponents;

        if (!allowsChildrenInCX && !isParent && element.Children.Count > 0)
        {
            bag.AddDiagnostics(
                element.Children.Report(
                    Diagnostic.ComponentDoesntAllowChildren(this)
                )
            );
        }
    }

    protected void ValidateProperties(
        ComponentState state,
        IDiagnosticBag bag
    )
    {
        foreach (var property in Properties)
        {
            ValidateProperty(state, property, bag);
        }
    }

    protected void ValidateProperty(
        ComponentState state,
        ComponentProperty property,
        IDiagnosticBag bag,
        bool? isOptional = null,
        bool? requiresValue = null
    )
    {
        var optional = isOptional ?? property.IsOptional;
        var requireValue = requiresValue ?? property.RequiresValue;

        var propertyValue = state.GetPropertyValue(property);

        if (
            (!optional && !propertyValue.IsSpecified) ||
            (requireValue && !propertyValue.HasValue)
        )
        {
            bag.AddDiagnostics(
                propertyValue.TextSpan.Report(
                    Diagnostic.RequiredPropertyNotSpecified(this, property)
                )
            );
        }
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
        IList<Diagnostic> diagnostics,
        CancellationToken token
    ) => Initialize(context, diagnostics, token);

    Result<string> IComponentNode.Emit(
        ComponentState state,
        ComponentEmitContext context,
        ComponentOptions options,
        CancellationToken token
    ) => Emit((T)state, context, options, token);
}