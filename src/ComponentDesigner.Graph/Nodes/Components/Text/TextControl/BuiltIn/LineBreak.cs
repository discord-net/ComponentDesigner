using ComponentDesigner.Parser;

namespace ComponentDesigner.Nodes.TextControls;

partial class TextControlElement
{
    public sealed class LineBreak(
        CXElement element,
        IReadOnlyList<TextControlElement> children
    ) : TextControlElement(element, children)
    {
        public override string Name => "Line Break";

        public override Result<TextControl> Render(
            IRendererContext context,
            TextControlOptions options,
            CancellationToken cancellationToken = default
        )
        {
            if (Children?.Count > 0)
                return Diagnostic
                    .ComponentDoesntAllowChildren(Name)
                    .At(element.Children);

            return new TextControl(
                element.LeadingTrivia,
                element.TrailingTrivia,
                Environment.NewLine,
                ValueContainsNewLines: true
            );
        }
    }
}