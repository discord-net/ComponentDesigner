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
        ValidateElementStructure(container, state, bag);
        ValidateProperty(container, state.GetPropertyValue(container.Id), bag);
        ValidateProperty(container, state.GetPropertyValue(container.AccentColor), bag);
        ReportDiagnosticsForUnknownProperties(container, state, bag);

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
        if (
            child is not IDynamicComponentNode
            and not ActionRowComponentNode
            and not TextDisplayComponentNode
            and not SectionComponentNode 
            and not MediaGalleryComponentNode
            and not SeparatorComponentNode
            and not FileComponentNode
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