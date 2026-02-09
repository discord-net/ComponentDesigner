using ComponentDesigner.Parser;
using ComponentDesigner.Parser.Util;
using ComponentDesigner.Util;

namespace ComponentDesigner.Nodes.Text.Controls;

public sealed class BoldTextControlElement(
    CXElement element,
    IReadOnlyList<TextControlElement> children
) : TextControlElement(element, children)
{
    public override string Name => "Bold";

    protected override Result<TextControl> Render(
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