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
        ValidateElementStructure(actionRow, state, bag);
        ValidateProperty(actionRow, state.GetPropertyValue(actionRow.Id), bag);
        ReportDiagnosticsForUnknownProperties(actionRow, state, bag);

        var components = state.GetPropertyValue(actionRow.Components);

        if (!state.HasGraphChildren && !components.HasValue)
        {
            bag.Add(
                state.TextSpan.Report(Diagnostic.ComponentRequiresAtLeastOneChild(actionRow))
            );

            return;
        }

        if (state.Children.Count > 0)
        {
            switch (state.Children[0].Component)
            {
                case ButtonComponentNode:
                {
                    for (var i = 1; i < Math.Min(5, state.Children.Count); i++)
                    {
                        var child = state.Children[i];

                        if (child.Component is not ButtonComponentNode and not IDynamicComponentNode)
                        {
                            bag.Add(
                                Diagnostic
                                    .InvalidChildOfComponent(actionRow, child.Component)
                                    .At(child.State.TextSpan)
                            );
                        }
                    }

                    // any remaining components are not allow
                    if (state.Children.Count > 5)
                    {
                        bag.Add(
                            Diagnostic
                                .TooManyChildren(actionRow, 5)
                                .At(
                                    CXTextSpan.FromBounds(
                                        state.Children[5].State.TextSpan.Start,
                                        state.Children[state.Children.Count - 1].State.TextSpan.End
                                    )
                                )
                        );
                    }

                    break;
                }

                case SelectMenuComponentNode when state.Children.Count > 1:
                {
                    // no other components are allowed
                    bag.Add(
                        Diagnostic
                            .TooManyChildren(actionRow, 5)
                            .At(
                                CXTextSpan.FromBounds(
                                    state.Children[1].State.TextSpan.Start,
                                    state.Children[state.Children.Count - 1].State.TextSpan.End
                                )
                            )
                    );

                    break;
                }

                case IDynamicComponentNode: break;

                default:
                    foreach (var invalidChild in state.Children.Where(x => !IsValidChild(x.Component)))
                    {
                        bag.Add(
                            Diagnostic
                                .InvalidChildOfComponent(actionRow, invalidChild.Component)
                                .At(invalidChild.State.TextSpan)
                        );
                    }

                    break;
            }
        }

        static bool IsValidChild(IComponentNode node)
            => node is IDynamicComponentNode
                or ButtonComponentNode
                or SelectMenuComponentNode;
    }
}