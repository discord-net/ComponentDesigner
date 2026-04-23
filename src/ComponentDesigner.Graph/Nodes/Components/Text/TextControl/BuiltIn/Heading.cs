using ComponentDesigner.Parser;

namespace ComponentDesigner.Nodes.TextControls;

partial class TextControlElement
{
    public enum HeadingVariant
    {
        H1,
        H2,
        H3
    }
    
    public sealed class Heading(
        CXElement element,
        HeadingVariant variant,
        IReadOnlyList<TextControlElement> children
    ) : TextControlElement(element, children)
    {
        public override string Name => variant.ToString();

        public override Result<TextControl> Render(
            IRenderContext context,
            TextControlOptions options,
            CancellationToken cancellationToken = default
        ) => GetHeadingPrefix()
            .Combine(
                RenderChildren(context, options, cancellationToken),
                (prefix, children) => new TextControl(
                    element.LeadingTrivia,
                    EnsureLineBreaks(element.TrailingTrivia),
                    $"{prefix} {RenderChildrenWithoutNewLines(children)}",
                    ValueContainsNewLines: false
                )
            );

        private Result<string> GetHeadingPrefix()
            => variant switch
            {
                HeadingVariant.H1 => "#",
                HeadingVariant.H2 => "##",
                HeadingVariant.H3 => "###",
                _ => Diagnostic
                    .UnknownComponentElement(element.Identifier)
                    .At(element.IdentifierTextSpanOrElementTextSpan)
            };
    }
}