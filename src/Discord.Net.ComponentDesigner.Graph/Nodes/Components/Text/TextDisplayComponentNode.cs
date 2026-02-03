using Discord.CX.Nodes.Text;
using Discord.CX.Parser;

namespace Discord.CX.Nodes;

public sealed record TextDisplayState(
    GraphNode GraphNode,
    ICXNode? CXNode,
    TextControlElement? Content
) : ComponentState(GraphNode, CXNode);

public class TextDisplayComponentNode : ComponentNode<TextDisplayState>
{
    public override string Name => "text-display";

    public override IReadOnlyList<string> Aliases => ["text"];

    protected override bool AllowChildrenInCX => true;

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
        CancellationToken token = default
    )
    {
        context.Push(
            this,
            cxNode: context.CXNode
        );
    }

    public override TextDisplayState? Initialize(
        ComponentNodeInitializationContext context,
        IList<Diagnostic> diagnostics,
        CancellationToken token = default
    ) => new TextDisplayState(
        context.GraphNode,
        context.CXNode,
        context.CXNode is CXElement element
            ? CreateTextControl(context.GraphContext, element, diagnostics, token)
            : null
    );

    private TextControlElement? CreateTextControl(
        IGraphContext context,
        CXElement element,
        IList<Diagnostic> diagnostics,
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

    public override Result<string> Emit(
        TextDisplayState state,
        ComponentEmitContext context,
        ComponentOptions options,
        CancellationToken cancellationToken = default
    )
    {
        var contentProperty = state.GetPropertyValue(Content);

        // the property is exclusive with the states text control
        if (contentProperty.IsSpecified && state.Content is not null)
        {
            return contentProperty.TextSpan.Report(
                Diagnostic.ChildSuppliedExclusivePropertyDuplicated(contentProperty.UsedName)
            );
        }

        if (!contentProperty.HasValue && state.Content is null)
        {
            return state.ElementIdentifierTextSpanOrBetter.Report(
                Diagnostic.RequiredPropertyNotSpecified(this, Content)
            );
        }

        var bag = DiagnosticBag.Get();

        ValidateProperty(state, Id, bag);
        ReportDiagnosticsForUnknownProperties(state, bag);

        return context
            .Renderer
            .RenderTextDisplay(
                context,
                this,
                state,
                state.GetPropertyValue(Id),
                state.GetPropertyValue(Content),
                cancellationToken
            );
    }
}