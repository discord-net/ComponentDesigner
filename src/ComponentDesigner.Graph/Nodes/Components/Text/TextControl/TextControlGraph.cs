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
        IRendererContext context,
        TextControlOptions options,
        CancellationToken cancellationToken = default
    ) => RootElements
        .Select(x => x.Render(context, options, cancellationToken))
        .FlattenAll()
        .Map(x => string.Join(string.Empty, x));
}