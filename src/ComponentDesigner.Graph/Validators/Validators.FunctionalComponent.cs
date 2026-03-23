using ComponentDesigner.Nodes;

namespace ComponentDesigner;

partial class Validators
{
    public static void ValidateFunctionalComponent(
        IComponentContext context,
        FunctionalComponentNode functionalComponent,
        FunctionalState state,
        IDiagnosticBag bag
    )
    {
        ValidateGenericComponent(functionalComponent, state, bag);
    }
}