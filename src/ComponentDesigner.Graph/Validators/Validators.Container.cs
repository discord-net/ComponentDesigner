using ComponentDesigner.Nodes;

namespace ComponentDesigner;

partial class Validators
{
    public static void ValidateContainer(
        IComponentContext context,
        ContainerComponentNode container,
        ComponentState state,
        IDiagnosticBag bag
    )
    {
        ValidateGenericComponent(container, state, bag);

        if (state.Children.Count is 0)
        {
            bag.Add(
                state.TextSpan.Report(Diagnostic.ComponentRequiresAtLeastOneChild(container))
            );
        }
        else
        {
            foreach (var child in state.Children)
                ValidateChildIsAllowedInContainer(container, state, bag, child.Component);
        }
    }

    private static void ValidateChildIsAllowedInContainer(
        ContainerComponentNode container,
        ComponentState state,
        IDiagnosticBag bag,
        IComponentNode child
    )
    {
        // TODO: rest of components
        if (
            child is not IDynamicComponentNode
            and not TextDisplayComponentNode
        )
        {
            bag.Add(
                state.TextSpan.Report(
                    Diagnostic.InvalidChildOfComponent(container, child)
                )
            );
        }
    }
}