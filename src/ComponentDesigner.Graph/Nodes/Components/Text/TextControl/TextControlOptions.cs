namespace ComponentDesigner.Nodes.TextControls;

public delegate Result<string> TextControlInterpolationRenderer(
    IRenderContext context,
    IInterpolationInfo info,
    out bool valueContainsNewlines
);

public sealed record TextControlOptions(
    TextControlInterpolationRenderer InterpolationRenderer
);