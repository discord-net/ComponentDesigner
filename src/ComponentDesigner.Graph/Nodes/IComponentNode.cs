namespace ComponentDesigner.Nodes;

public interface IComponentNode : IEquatable<IComponentNode>
{
    string Name { get; }
    IReadOnlyList<string> Aliases { get; }
    bool IsParentOfOtherComponents { get; }
    IReadOnlyList<ComponentProperty> Properties { get; }
    bool IsUserAccessible { get; }
    bool AllowChildrenInCX { get; }
    bool HasExternalDependencies { get; }

    ComponentState? Initialize(
        ComponentNodeInitializationContext context,
        IDiagnosticBag diagnostics,
        CancellationToken cancellationToken = default
    );

    ComponentState UpdateState(
        ComponentState state,
        IComponentContext context,
        IDiagnosticBag diagnostics,
        CancellationToken cancellationToken = default);

    void RegisterGraphNode(ComponentGraphInitializationContext context, CancellationToken cancellationToken = default);

    Result<RenderedComponent> Emit(
        ComponentState state,
        ComponentEmitContext context,
        ComponentOptions options,
        CancellationToken cancellationToken = default
    );
}