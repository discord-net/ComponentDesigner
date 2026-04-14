namespace ComponentDesigner.Nodes;

public delegate Result<T> ComponentPropertyValueTransformer<T>(
    IRendererContext context,
    ComponentPropertyValue value,
    CancellationToken cancellationToken = default
);

public delegate Result<TResult> ComponentPropertyValueTransformer<TResult, in TValue>(
    IRendererContext context,
    TValue value,
    CancellationToken cancellationToken = default
) where TValue : ComponentPropertyValue;