using ComponentDesigner.Parser;
using ComponentDesigner.Parser.Util;
using ComponentDesigner.Util;

namespace ComponentDesigner.Nodes.TextControls;

partial class TextControlElement
{
    public sealed class Bold(
        CXElement element,
        IReadOnlyList<TextControlElement> children
    ) : TextControlElement(element, children)
    {
        public override string Name => "Bold";

        public override Result<TextControl> Render(
            IRendererContext context,
            TextControlOptions options,
            CancellationToken token = default
        ) => Join(RenderChildren(context, options, token))
            .Map(children =>
                new TextControl(
                    element.LeadingTrivia,
                    element.TrailingTrivia,
                    $"**{($"{children.LeadingTrivia.ToIndentationOnly()}{children.Value}".NormalizeIndentation())}**",
                    children.ValueContainsNewLines
                )
            );
    }
}