using ComponentDesigner.Nodes;

namespace ComponentDesigner;

partial class Validators
{
    public static void ValidateSeparator(
        IComponentContext context,
        SeparatorComponentNode separator,
        ComponentState state,
        IDiagnosticBag bag
    )
    {
        ValidateGenericComponent(context, separator, state, bag);
    }
}