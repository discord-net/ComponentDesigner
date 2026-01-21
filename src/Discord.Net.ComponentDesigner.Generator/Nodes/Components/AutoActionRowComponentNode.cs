using System.Collections.Generic;
using System.Linq;
using Discord.CX.Nodes.Components.SelectMenus;
using Discord.CX.Parser;
using Microsoft.CodeAnalysis.Text;

namespace Discord.CX.Nodes.Components;

public sealed class AutoActionRowComponentNode : ActionRowComponentNode
{
    public static readonly AutoActionRowComponentNode Instance = new();
    protected override bool IsUserAccessible => false;

    public static bool AddPossibleAutoRowNode(
        ComponentNode target,
        ComponentGraphInitializationContext context
    )
    {
        // if we're a root node, no auto row is added
        if (context.ParentGraphNode is null)
            return false;

        // if we're in a row already, don't add an auto row
        if (context.ParentGraphNode.Inner is ActionRowComponentNode)
            return false;

        // grab the sibling graph node
        var sibling = context.ParentGraphNode.Children.LastOrDefault();
        
        if (sibling?.Inner is AutoActionRowComponentNode)
        {
            var canAddToRow = target switch
            {
                // 5 buttons per row, if the row is all buttons
                ButtonComponentNode
                    => sibling.Children.Count < 5 &&
                       sibling.Children.All(x => x.Inner is ButtonComponentNode),

                // the auto row is empty
                SelectMenuComponentNode
                    => !sibling.HasChildren,

                _ => false
            };

            if (canAddToRow)
            {
                // no sibling means we're the first node being added to the graph, and we can create an auto row
                context.Push(target, parent: sibling);
                return true;
            }
        }

        // if the above doesn't succeed, we can simply create a new auto row
        context.Push(Instance, children: [(CXNode)context.CXNode]);
        return true;
    }
    
    public override ComponentState? Create(
        ComponentStateInitializationContext context,
        IList<DiagnosticInfo> diagnostics
    ) => new(context.GraphNode, context.CXNode);

    public override void Validate(ComponentState state, IComponentContext context, IList<DiagnosticInfo> diagnostics)
    {
        if (!context.IsMessageContext)
        {
            diagnostics.Add(
                Diagnostics.ComponentNotAllowedInContext(Name, context.CX.Kind),
                state.Source
            );
        }
        
        if (!context.Options.EnableAutoRows)
        {
            diagnostics.Add(
                Diagnostics.AutoRowsDisabled,
                state.HasChildren
                    ? TextSpan.FromBounds(
                        state.Children[0].SourceCXNode.Span.Start,
                        state.Children[state.Children.Count - 1].SourceCXNode.Span.End
                    )
                    : state.Source.Span
            );
        }
    }
}