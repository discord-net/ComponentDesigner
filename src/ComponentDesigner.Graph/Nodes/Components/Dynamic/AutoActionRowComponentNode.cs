namespace ComponentDesigner.Nodes;

public sealed class AutoActionRowComponentNode : ActionRowComponentNode
{
    public override string Name => "<auto action row>";

    public static readonly AutoActionRowComponentNode Instance = new();
    public override bool IsUserAccessible => false;

    public static bool TryInsertActionRow(
        IComponentNode target,
        ComponentGraphInitializationContext context
    )
    {
        if (context.ParentGraphNode is null)
        {
            // root node, don't attempt a row
            return false;
        }

        if (context.ParentGraphNode.Component is ActionRowComponentNode)
        {
            // we're in a row already
            return false;
        }

        var sibling = context.ParentGraphNode.Children.LastOrDefault();

        if (sibling?.Component is AutoActionRowComponentNode)
        {
            // the original sibling node of the target was added to an auto action row

            var canAddToRow = target switch
            {
                // 5 buttons per row, if the row is all buttons
                ButtonComponentNode
                    => sibling.Children.Count < 5 &&
                       sibling.Children.All(x => x.Component is ButtonComponentNode),

                // only one select menu
                SelectMenuComponentNode => !sibling.HasChildren,

                _ => false
            };

            if (canAddToRow)
            {
                // we can push the target to the sibling auto action row
                context.Push(target, cxNode: context.CXNode, parent: sibling);
                return true;
            }
        }

        /*
         * can't push the node if we cannot resolve it later, although this could be rectified
         * if the initialization request API changes to allow children to be specified by
         * other means. it works for now in the case of buttons and select menus.
         */
        if (context.CXNode is null) return false;

        // we can create a new auto row
        context.Push(Instance, children: [context.CXNode]);
        return true;
    }
    
    public override Result<RenderedComponent> Emit(
        ComponentState state,
        ComponentEmitContext context,
        ComponentOptions options,
        CancellationToken cancellationToken = default
    ) => ValidateAndRender(
        this,
        state,
        context,
        options,
        Validators.ValidateAutoActionRow,
        context.Renderer.RenderActionRow,
        cancellationToken
    );
}