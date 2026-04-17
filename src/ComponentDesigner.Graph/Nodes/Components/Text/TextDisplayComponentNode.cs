using ComponentDesigner.Nodes.TextControls;
using ComponentDesigner.Parser;

namespace ComponentDesigner.Nodes;

public sealed record TextDisplayState : ComponentState
{
    public TextDisplayState(ComponentNodeInitializationContext context, CancellationToken cancellationToken)
        : base(context, cancellationToken)
    {
    }

    public TextDisplayState(GraphNode graphNode) : base(graphNode)
    {
        
    }
}

public class TextDisplayComponentNode : ComponentNode<TextDisplayState>
{
    public override string Name => "text-display";

    public override IReadOnlyList<string> Aliases => ["text"];

    public override ComponentTargetType Target => ComponentTargetType.Any;
    
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
                isOptional: true,
                kind: ComponentPropertyValueKind.SyntaxValue | ComponentPropertyValueKind.Component
            )
        ];
    }

    public override void RegisterGraphNode(
        ComponentGraphInitializationContext context,
        CancellationToken cancellationToken = default
    ) => base.RegisterGraphNode(context, includeElementChildren: false, cancellationToken);

    public override TextDisplayState? CreateState(
        ComponentNodeInitializationContext context,
        IDiagnosticBag diagnostics,
        CancellationToken cancellationToken = default
    )
    {
        if (context.CXNode is not CXElement element) return null;

        var state = new TextDisplayState(context, cancellationToken);

        if (element.Children.Count > 0)
        {
            context.Push(
                TextControlNode.Instance,
                cxNode: element.Children,
                parent: context.GraphNode,
                cancellationToken: cancellationToken
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

    public override void Validate(
        IComponentContext context, TextDisplayState state, IDiagnosticBag bag,
        CancellationToken cancellationToken = default
    ) => Validators.ValidateTextDisplay(context, this, state, bag);

    public override Result<RenderedComponent> Render(
        ComponentEmitContext context,
        TextDisplayState state,
        ComponentOptions options,
        CancellationToken cancellationToken = default
    ) => context.Renderer.RenderTextDisplay(context, this, state, options.TypingContext, cancellationToken);
}