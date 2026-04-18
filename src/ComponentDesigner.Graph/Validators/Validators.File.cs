using ComponentDesigner.Nodes;

namespace ComponentDesigner;

partial class Validators
{
    public static void ValidateFile(
        IComponentContext context,
        FileComponentNode file,
        ComponentState state,
        IDiagnosticBag bag
    )
    {
        ValidateGenericComponent(context, file, state, bag);
    }
}