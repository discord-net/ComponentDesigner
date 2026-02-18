namespace ComponentDesigner;

public sealed record GraphOptions(
    bool AllowAutoRows = false,
    bool AllowAutoTextDisplays = false
)
{
    public static readonly GraphOptions Default = new();
}