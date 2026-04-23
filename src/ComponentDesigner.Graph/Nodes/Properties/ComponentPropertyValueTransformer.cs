namespace ComponentDesigner.Nodes;

public delegate Result<TResult> ComponentPropertyValueTransformer<TResult>(
    IComponentContext context,
    ComponentPropertyValue value,
    CancellationToken cancellationToken = default
);

public delegate Result<TResult> ComponentPropertyValueTransformer<in TContext, TResult>(
    TContext context,
    ComponentPropertyValue value,
    CancellationToken cancellationToken = default
) where TContext : IComponentContext;

public delegate Result<TResult> ComponentPropertyValueTransformer<in TContext, TResult, in TValue>(
    TContext context,
    TValue value,
    CancellationToken cancellationToken = default
) where TContext : IComponentContext where TValue : ComponentPropertyValue;