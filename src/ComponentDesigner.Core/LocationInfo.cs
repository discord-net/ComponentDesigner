namespace ComponentDesigner;

public sealed record LocationInfo(
    string? FilePath,
    CXTextSpan TextSpan,
    LinePositionSpan LineSpan
)
{
    public override string ToString() => $"{FilePath} @ {LineSpan} : {TextSpan}";
}