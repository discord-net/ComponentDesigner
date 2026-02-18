using ComponentDesigner.Nodes;

namespace ComponentDesigner;

partial class Validators
{
    public static void ValidateAutoActionRow(
        IComponentContext context,
        AutoActionRowComponentNode actionRow,
        ComponentState state,
        IDiagnosticBag bag
    )
    {
        if (!context.Options.AllowAutoRows)
        {
            bag.Add(Diagnostic.FeatureAutoTextDisplaysDisabled.At(state.TextSpan));
        }
    }
}