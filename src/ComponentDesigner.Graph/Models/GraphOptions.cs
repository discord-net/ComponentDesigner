namespace ComponentDesigner;

public sealed record GraphOptions(
    bool AllowAutoRows,
    bool AllowAutoTextDisplays
)
{
    public static readonly GraphOptions Default = new(false, false);
}