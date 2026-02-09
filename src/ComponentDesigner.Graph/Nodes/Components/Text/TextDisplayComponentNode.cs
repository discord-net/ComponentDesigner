using ComponentDesigner.Nodes.Text;
using ComponentDesigner.Parser;

namespace ComponentDesigner.Nodes;

public sealed record TextDisplayState(
    GraphNode GraphNode,
    ICXNode? CXNode,
    TextControlElement? Content
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
    ) => new TextDisplayState(
        context.GraphNode,
        context.CXNode,
        context.CXNode is CXElement element
            ? CreateTextControl(context.GraphContext, element, diagnostics, cancellationToken)
            : null
    );

    private TextControlElement? CreateTextControl(
        IGraphContext context,
        CXElement element,
        IDiagnosticBag diagnostics,
        CancellationToken token
    )
    {
        if (element.Children.Count is 0) return null;

        using var enumerator = GraphNodeEnumerator.GetNext(element.Children).GetEnumerator();

        if (!enumerator.MoveNext()) return null;

        var textControlWasCreated = TextControlElement.TryCreate(
            context,
            enumerator,
            diagnostics,
            out var textControlElement,
            out var hasMoreInEnumerator,
            token
        );

        if (!textControlWasCreated)
        {
            // all children are invalid
            foreach (var child in element.Children)
            {
                diagnostics.Add(
                    child.Report(
                        Diagnostic.InvalidChildOfComponent(this, child)
                    )
                );
            }
        }
        else if (hasMoreInEnumerator && enumerator.Current is not null)
        {
            do
            {
                diagnostics.Add(
                    enumerator.Current.Report(
                        Diagnostic.InvalidChildOfComponent(this, enumerator.Current)
                    )
                );
            } while (enumerator.MoveNext());
        }

        return textControlElement;
    }

    public override Result<RenderedComponent> Emit(
        TextDisplayState state,
        ComponentEmitContext context,
        ComponentOptions options,
        CancellationToken cancellationToken = default
    )=> ValidateAndRender(
        this,
        state,
        context,
        options,
        Validators.ValidateTextDisplay,
        context.Renderer.RenderTextDisplay,
        cancellationToken
    );
}