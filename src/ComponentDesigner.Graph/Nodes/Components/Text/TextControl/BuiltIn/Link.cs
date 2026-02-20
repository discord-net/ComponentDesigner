using System.Text;
using ComponentDesigner.Parser;
using ComponentDesigner.Util;

namespace ComponentDesigner.Nodes.TextControls;

partial class TextControlElement
{
    public sealed class Link(
        CXElement element,
        IReadOnlyList<TextControlElement> children
    ) : TextControlElement(element, children)
    {
        public override string Name => "Link";

        public override Result<TextControl> Render(
            IRendererContext context,
            TextControlOptions options,
            CancellationToken cancellationToken = default
        ) => ExtractLink(context, options)
            .Combine(
                Join(RenderChildren(context, options, cancellationToken)),
                (link, children) => new TextControl(
                    element,
                    $"[{($"{children.LeadingTrivia.ToIndentationOnly()}{children.Value}".NormalizeIndentation())}]({link})",
                    children.ValueContainsNewLines
                )
            );

        private Result<string> ExtractLink(
            IRendererContext context,
            TextControlOptions options
        )
        {
            CXAttribute? href = null;

            foreach (var attribute in element.Attributes)
            {
                if (attribute.Identifier is not "href" and not "url") continue;

                if (href is not null)
                {
                    return Diagnostic
                        .DuplicatePropertyValue(attribute.Identifier)
                        .At(attribute);
                }

                href = attribute;
            }

            if (href is null)
                return Diagnostic
                    .RequiredPropertyNotSpecified(element.Identifier, "href")
                    .At(element.IdentifierTextSpanOrElementTextSpan);

            if(!TryGetTextBasedValue(href.Value, context, options, out var value))
                return Diagnostic
                    .InvalidPropertyValue(href.Identifier, href.Value?.GetType().Name ?? "null")
                    .At(href.Value?.TextSpan ?? href.TextSpan);

            return value;
        }
    }
}