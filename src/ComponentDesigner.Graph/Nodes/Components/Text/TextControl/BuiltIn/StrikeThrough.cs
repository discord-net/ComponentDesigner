using ComponentDesigner.Parser;
using ComponentDesigner.Util;

namespace ComponentDesigner.Nodes.TextControls;

partial class TextControlElement
{
    public sealed class StrikeThrough(
        CXElement element,
        IReadOnlyList<TextControlElement> children
    ) : TextControlElement(element, children)
    {
        public override string Name => "Strike Through";

        public override Result<TextControl> Render(
            IRenderContext context,
            TextControlOptions options,
            CancellationToken cancellationToken = default
        ) => Join(RenderChildren(context, options, cancellationToken))
            .Map(children => new TextControl(
                element,
                $"~~{($"{children.LeadingTrivia.ToIndentationOnly()}{children.Value}".NormalizeIndentation())}~~",
                children.ValueContainsNewLines
            ));
    }
}