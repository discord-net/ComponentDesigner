using ComponentDesigner.Parser;
using ComponentDesigner.Util;

namespace ComponentDesigner.Nodes.TextControls;

partial class TextControlElement
{
    public sealed class Italic(
        CXElement element,
        IReadOnlyList<TextControlElement> children
    ) : TextControlElement(element, children)
    {
        public override string Name => "Italic";

        public override Result<TextControl> Render(
            IRenderContext context,
            TextControlOptions options,
            CancellationToken cancellationToken = default
        ) => Join(RenderChildren(context, options, cancellationToken))
            .Map(children => new TextControl(
                element.LeadingTrivia,
                element.TrailingTrivia,
                $"_{($"{children.LeadingTrivia.ToIndentationOnly()}{children.Value}".NormalizeIndentation())}_",
                children.ValueContainsNewLines
            ));
    }
}