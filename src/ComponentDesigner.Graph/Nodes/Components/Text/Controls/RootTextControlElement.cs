using System.Text;
using ComponentDesigner.Parser;
using ComponentDesigner.Util;

namespace ComponentDesigner.Nodes.Text.Controls;

public sealed class RootTextControlElement : TextControlElement
{
    public override string Name { get; } = "Root";

    public bool HasInterpolations { get; }
    public int InterpolationDollarCount { get; }

    private RootTextControlElement(
        CXTextSpan textSpan,
        IReadOnlyList<TextControlElement> children,
        bool hasInterpolations,
        int interpolationDollarCount
    ) : base(textSpan, children)
    {
        HasInterpolations = hasInterpolations;
        InterpolationDollarCount = interpolationDollarCount;
    }

    public static RootTextControlElement Create(
        IReadOnlyList<CXToken> tokens,
        IReadOnlyList<TextControlElement> children
    )
    {
        CalculateInterpolationDetails(tokens, out var hasInterpolations, out var interpolationDollarCount);

        var textSpan = tokens.Count is 0
            ? default
            : CXTextSpan.FromBounds(tokens[0].Span.Start, tokens[tokens.Count - 1].Span.End);

        return new RootTextControlElement(
            textSpan,
            children,
            hasInterpolations,
            interpolationDollarCount
        );
    }

    protected override Result<TextControl> Render(
        IRendererContext context,
        TextControlOptions options,
        CancellationToken token = default
    )
    {
        var startInterpolation = HasInterpolations
            ? new string('{', InterpolationDollarCount)
            : string.Empty;

        var endInterpolation = HasInterpolations
            ? new string('}', InterpolationDollarCount)
            : string.Empty;

        options = new TextControlOptions(
            startInterpolation,
            endInterpolation,
            options.AsCSharpString
        );
        
        var result = Join(RenderChildren(context, options)).Map(x => x with
        {
            LeadingTrivia = x.LeadingTrivia.TrimLeadingSyntaxIndentation(),
            TrailingTrivia = x.TrailingTrivia.TrimTrailingSyntaxIndentation()
        });

        if (options.AsCSharpString) result = result.Map(AsCSharpString);

        return result;
    }

    private Result<TextControl> AsCSharpString(TextControl children)
    {
        var quoteCount = (StringGenerator.GetSequentialQuoteCount(children.Value) + 1) switch
        {
            2 => 3,
            var r => r
        };
        
        var isMultiline = children.ContainsNewLines || quoteCount > 1;
        var isMultilineInterpolation = isMultiline && HasInterpolations;

        if (isMultiline)
            quoteCount = Math.Max(3, quoteCount);

        var dollars = HasInterpolations
            ? new string(
                '$',
                InterpolationDollarCount
            )
            : string.Empty;

        var quotes = new string('"', quoteCount);

        var pad = isMultilineInterpolation
            ? new string(' ', InterpolationDollarCount)
            : string.Empty;

        using var _ = ObjectPool<StringBuilder>.GetScoped(out var sb);
        sb.Clear();

        // start on newline if it's a multi-line string
        if (isMultiline) sb.AppendLine();

        sb.Append(dollars).Append(quotes);

        if (isMultiline) sb.AppendLine();

        var value = children.ToString().NormalizeIndentation().Trim(['\r', '\n']);

        if (isMultilineInterpolation)
            value = value.Indent(InterpolationDollarCount);

        sb.Append(value);

        if (isMultiline) sb.AppendLine();

        if (isMultilineInterpolation) sb.Append(pad);
        sb.Append(quotes);

        return new TextControl(
            LexedCXTrivia.Empty,
            LexedCXTrivia.Empty,
            sb.ToString(),
            isMultiline
        );
    }

    private static void CalculateInterpolationDetails(
        IReadOnlyList<CXToken> tokens,
        out bool hasInterpolations,
        out int interpolationDollarCount
    )
    {
        hasInterpolations = false;
        interpolationDollarCount = 0;

        for (var i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];

            switch (token.Kind)
            {
                case CXTokenKind.Interpolation:
                    hasInterpolations = true;
                    break;
                case CXTokenKind.Text:
                    interpolationDollarCount = Math
                        .Max(
                            interpolationDollarCount,
                            StringGenerator.GetInterpolationDollarRequirement(token.Value)
                        );
                    break;
            }
        }
    }
}