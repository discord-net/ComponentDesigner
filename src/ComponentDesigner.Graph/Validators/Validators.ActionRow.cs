using ComponentDesigner.Nodes;

namespace ComponentDesigner;

partial class Validators
{
    public static void ValidateActionRow(
        IComponentContext context,
        ActionRowComponentNode actionRow,
        ComponentState state,
        IDiagnosticBag bag
    )
    {
        ValidateGenericComponent(actionRow, state, bag);

        if (!state.HasGraphChildren)
        {
            bag.Add(
                state.TextSpan.Report(Diagnostic.ComponentRequiresAtLeastOneChild(actionRow))
            );

            return;
        }

        // TODO: validate children

        static bool IsValidChild(IComponentNode node)
            => node is IDynamicComponentNode
                or ButtonComponentNode
                or SelectMenuComponentNode;
    }
}