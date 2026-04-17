using ComponentDesigner.Nodes;

namespace ComponentDesigner;

partial class Validators
{
    public static void ValidateTextDisplay(
        IComponentContext context,
        TextDisplayComponentNode textDisplay,
        TextDisplayState state,
        IDiagnosticBag bag
    )
    {
        ValidateGenericComponent(textDisplay, state, bag);
    }
}