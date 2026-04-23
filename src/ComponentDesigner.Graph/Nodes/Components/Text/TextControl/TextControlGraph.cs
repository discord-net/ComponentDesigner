using System.Text;
using ComponentDesigner.Parser;

namespace ComponentDesigner.Nodes.TextControls;

public readonly record struct TextControlGraph(
    IReadOnlyList<TextControlElement> RootElements,
    bool ContainsInterpolations,
    int InterpolationDollarSignRequirement
)
{
    public CXTextSpan TextSpan => RootElements.Count is 0
        ? default
        : CXTextSpan.FromBounds(
            RootElements[0].TextSpan.Start,
            RootElements[RootElements.Count - 1].TextSpan.End
        );

    public Result<string> Render(
        IRenderContext context,
        TextControlOptions options,
        CancellationToken cancellationToken = default
    )
    {
        return Collect(RootElements.Select(control => control.Render(context, options, cancellationToken)))
            .Map(x => x.ToString());
        
        static Result<TextControl> Collect(
            IEnumerable<Result<TextControl>> controls
        )
        {
            using var result = Result<TextControl>.Builder;
            using var _ = StringBuilder.Pooled(out var sb);
            var containsNewLines = false;

            TextControl? first = null;
            TextControl? last = null;

            foreach (var controlResult in controls)
            {
                if (last is not null)
                {
                    sb.Append(last.Value.TrailingTrivia);
                    containsNewLines |= last.Value.TrailingTrivia.ContainsNewlines;
                }

                result.AddDiagnostics(controlResult.Diagnostics);

                if (!controlResult.HasValue)
                    continue;

                var control = controlResult.Value;

                first ??= control;

                if (sb.Length is not 0)
                {
                    sb.Append(control.LeadingTrivia);
                    containsNewLines |= control.LeadingTrivia.ContainsNewlines;
                }

                sb.Append(control.Value);
                containsNewLines |= control.ValueContainsNewLines;

                last = control;
            }

            return result
                .WithValue(
                    new TextControl(
                        LeadingTrivia: first?.LeadingTrivia ?? LexedCXTrivia.Empty,
                        TrailingTrivia: last?.TrailingTrivia ?? LexedCXTrivia.Empty,
                        Value: sb.ToString(),
                        ValueContainsNewLines: containsNewLines
                    )
                )
                .Build();
        }
    }
}