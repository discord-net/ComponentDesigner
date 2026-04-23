using System.Diagnostics.CodeAnalysis;

namespace ComponentDesigner.Nodes;

public interface IComponentNode : IEquatable<IComponentNode>
{
    string Name { get; }
    IReadOnlyList<string> Aliases { get; }
    IReadOnlyList<ComponentProperty> Properties { get; }
    ComponentTargetType Target { get; }
    bool IsUserAccessible { get; }
    bool HasExternalDependencies { get; }

    ComponentState? CreateState(
        ComponentNodeInitializationContext context,
        IDiagnosticBag diagnostics,
        CancellationToken cancellationToken = default
    );

    ComponentState? UpdateState(
        ComponentState state,
        IComponentContext context,
        IDiagnosticBag diagnostics,
        CancellationToken cancellationToken = default);

    void RegisterGraphNode(ComponentGraphInitializationContext context, CancellationToken cancellationToken = default);

    void Validate(
        IComponentContext context, 
        ComponentState state,
        IDiagnosticBag bag,
        CancellationToken cancellationToken = default
    );

    Result<TRender> Render<TRender>(
        IRenderContext<TRender> context,
        ComponentState state,
        CancellationToken cancellationToken = default
    );
}