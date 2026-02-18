using ComponentDesigner.Nodes.TextControls;
using ComponentDesigner.Parser;

namespace ComponentDesigner.Nodes;

public sealed record TextDisplayState(
    GraphNode GraphNode,
    ICXNode? CXNode
) : ComponentState(GraphNode, CXNode);

public class TextDisplayComponentNode : ComponentNode<TextDisplayState>
{
    public override string Name => "text-display";

    public override IReadOnlyList<string> Aliases => ["text"];

    public override bool AllowChildrenInCX => true;

    public override IReadOnlyList<ComponentProperty> Properties { get; }

    public ComponentProperty Id { get; }
    public ComponentProperty Content { get; }

    public TextDisplayComponentNode()
    {
        Properties =
        [
            Id = ComponentProperty.Id,
            Content = new(
                name: "content",
                isOptional: true
            )
        ];
    }

    public override void RegisterGraphNode(
        ComponentGraphInitializationContext context,
        CancellationToken cancellationToken = default
    )
    {
        context.Push(
            this,
            cxNode: context.CXNode
        );
    }

    public override TextDisplayState? Initialize(
        ComponentNodeInitializationContext context,
        IDiagnosticBag diagnostics,
        CancellationToken cancellationToken = default
    )
    {
        if (context.CXNode is not CXElement element) return null;

        var state = new TextDisplayState(context.GraphNode, element);

        if (element.Children.Count > 0)
        {
            context.Push(
                TextControlNode.Instance,
                cxNode: element.Children,
                parent: context.GraphNode
            );

            // we should expect one child
            if (context.GraphNode.Children.Count > 1)
            {
                diagnostics.Add(
                    Diagnostic
                        .OnlyOneChildAllowed(this)
                        .At(CXTextSpan.From(context.GraphNode.Children, start: 1))
                );
            }
            else if (context.GraphNode.Children.Count is 1)
            {
                state.SetPropertyValueToChild(Content, context.GraphNode.Children[0]);
            } 
        }

        return state;
    }


    public override Result<RenderedComponent> Emit(
        TextDisplayState state,
        ComponentEmitContext context,
        ComponentOptions options,
        CancellationToken cancellationToken = default
    ) => ValidateAndRender(
        this,
        state,
        context,
        options,
        Validators.ValidateTextDisplay,
        context.Renderer.RenderTextDisplay,
        cancellationToken
    );
}