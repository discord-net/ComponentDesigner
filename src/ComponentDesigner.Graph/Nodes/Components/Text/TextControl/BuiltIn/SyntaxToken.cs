using ComponentDesigner.Parser;

namespace ComponentDesigner.Nodes.TextControls;

partial class TextControlElement
{
    public sealed class SyntaxToken(CXToken token) : TextControlElement(token)
    {
        public override string Name => token.Kind.ToString();

        public override Result<TextControl> Render(
            IRenderContext context,
            TextControlOptions options,
            CancellationToken cancellationToken = default
        ) => RenderToken(context, token, options, out var valueContainsNewLines)
            .Map(str => new TextControl(
                token,
                str,
                valueContainsNewLines
            ));

        private static Result<string> RenderToken(
            IRenderContext context,
            CXToken token,
            TextControlOptions options,
            out bool containsNewLines
        )
        {
            if (token.InterpolationIndex is not {} index)
            {
                // simple token
                containsNewLines = token.Value.Contains('\n');
                return token.Value;
            }

            return options.InterpolationRenderer(
                context,
                context.GetInterpolationInfo(index),
                out containsNewLines
            );
        }
    }
}