namespace Discord.CX.Nodes;

public interface IComponentNode : IEquatable<IComponentNode>
{
    string Name { get; }
    IReadOnlyList<string> Aliases { get; }
    bool IsParentOfOtherComponents { get; }
    IReadOnlyList<ComponentProperty> Properties { get; }
    bool IsUserAccessible { get; }

    ComponentState? Initialize(ComponentNodeInitializationContext context, IList<Diagnostic> diagnostics,
        CancellationToken token = default);

    void RegisterGraphNode(ComponentGraphInitializationContext context, CancellationToken token = default);

    Result<string> Emit(
        ComponentState state,
        ComponentEmitContext context,
        ComponentOptions options,
        CancellationToken token = default
    );
}