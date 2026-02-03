namespace Discord.CX.Nodes;

public sealed class AutoTextDisplayComponentNode : TextDisplayComponentNode
{
    public static readonly AutoTextDisplayComponentNode Instance = new();

    public override bool IsUserAccessible => false;

    public override void RegisterGraphNode(
        ComponentGraphInitializationContext context,
        CancellationToken token = default
    ) => throw new InvalidOperationException("Auto nodes don't use default graph initialization");

    public override TextDisplayState? Initialize(
        ComponentNodeInitializationContext context,
        IList<Diagnostic> diagnostics,
        CancellationToken token = default
    ) => throw new InvalidOperationException("Auto nodes don't use default state creation");

    public override Result<string> Emit(
        TextDisplayState state,
        ComponentEmitContext context,
        ComponentOptions options,
        CancellationToken cancellationToken = default
    )
    {
        if (state.Content is null)
        {
            return state.TextSpan.Report(
                Diagnostic.RequiredPropertyNotSpecified(this, Content)
            );
        }

        return base.Emit(state, context, options, cancellationToken);
    }
}