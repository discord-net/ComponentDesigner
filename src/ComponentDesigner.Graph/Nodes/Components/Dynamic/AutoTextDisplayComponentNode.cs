namespace ComponentDesigner.Nodes;

public sealed class AutoTextDisplayComponentNode : TextDisplayComponentNode
{
    public static readonly AutoTextDisplayComponentNode Instance = new();

    public override bool IsUserAccessible => false;

    public override void RegisterGraphNode(
        ComponentGraphInitializationContext context,
        CancellationToken cancellationToken = default
    ) => throw new InvalidOperationException("Auto nodes don't use default graph initialization");

    public override TextDisplayState? Initialize(
        ComponentNodeInitializationContext context,
        IDiagnosticBag diagnostics,
        CancellationToken cancellationToken = default
    ) => throw new InvalidOperationException("Auto nodes don't use default state creation");
}