using System.Diagnostics.CodeAnalysis;

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

    bool TryGetProperty(string name, [MaybeNullWhen(false)] out ComponentProperty property);
    
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

    void Validate(
        IComponentContext context, ComponentState state, IDiagnosticBag bag,
        CancellationToken cancellationToken = default
    );

    Result<RenderedComponent> Render(
        ComponentEmitContext context, ComponentState state, ComponentOptions options,
        CancellationToken cancellationToken = default
    );

    // Result<RenderedComponent> Emit(
    //     ComponentState state,
    //     ComponentEmitContext context,
    //     ComponentOptions options,
    //     CancellationToken cancellationToken = default
    // );
}