using ComponentDesigner.Parser;

namespace ComponentDesigner.Nodes.Text.Controls;

public sealed class ScalarTextControlElement(CXToken token) : TextControlElement(token)
{
    public override string Name => token.Kind.ToString();

    protected override Result<TextControl> Render(
        IRendererContext context,
        TextControlOptions options,
        CancellationToken cancellationToken = default
    ) => new TextControl(
        token.LeadingTrivia,
        token.TrailingTrivia,
        Value: RenderToken(context, token, options, out var valueContainsNewLines),
        ValueContainsNewLines: valueContainsNewLines
    );

    private static string RenderToken(
        IRendererContext context,
        CXToken token,
        TextControlOptions options,
        out bool containsNewLines
    )
    {
        if (token.InterpolationIndex is { } index)
        {
            var info = context.GetInterpolationInfo(index);

            if (info.ConstantValue.IsSpecified)
            {
                var value = info.ConstantValue.Value?.ToString() ?? string.Empty;
                containsNewLines = value.Contains('\n');
                return value;
            }

            containsNewLines = false;
            return $"{options.StartInterpolationMarker}{
                context.GetReferenceToDesignerValue(info)
            }{options.EndInterpolationMarker}";
        }

        containsNewLines = token.Value.Contains('\n');
        return token.Value;
    }
}