using ComponentDesigner.Parser;
using ComponentDesigner.Util;

namespace ComponentDesigner.Nodes.TextControls;

partial class TextControlElement
{
    public sealed class Quote(
        CXElement element,
        IReadOnlyList<TextControlElement> children
    ) : TextControlElement(element, children)
    {
        public override string Name => "Quote";

        public override Result<TextControl> Render(
            IRendererContext context,
            TextControlOptions options,
            CancellationToken cancellationToken = default
        ) => Join(RenderChildren(context, options, cancellationToken))
            .Map(children =>
            {
                var value = children.Value;

                if (children.ValueContainsNewLines)
                    value = value.Replace("\n", "\n> ");
                
                value = $"{children.LeadingTrivia.ToIndentationOnly()}{value}".NormalizeIndentation();
        
                return new TextControl(
                    element,
                    $"> {value}",
                    children.ValueContainsNewLines
                );
            });
    }
}