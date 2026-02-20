using ComponentDesigner.Parser;

namespace ComponentDesigner.Nodes.TextControls;

partial class TextControlElement
{
    public sealed class SubText(
        CXElement element,
        IReadOnlyList<TextControlElement> children
    ) : TextControlElement(element, children)
    {
        public override string Name => "Sub Text";

        public override Result<TextControl> Render(
            IRendererContext context,
            TextControlOptions options,
            CancellationToken cancellationToken = default
        ) => RenderChildrenWithoutNewLines(context, options, cancellationToken)
            .Map(children => new TextControl(
                element.LeadingTrivia,
                EnsureLineBreaks(element.TrailingTrivia),
                $"-# {children}",
                ValueContainsNewLines: false
            ));
    }
}