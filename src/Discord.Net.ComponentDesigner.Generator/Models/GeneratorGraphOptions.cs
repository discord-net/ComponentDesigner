namespace ComponentDesigner;

public sealed record GeneratorGraphOptions(
    bool AllowAutoRows = false,
    bool AllowAutoTextDisplays = false,
    string? ProjectDirectory = null
) : IGraphOptions
{
    public static readonly GeneratorGraphOptions Default = new();
}