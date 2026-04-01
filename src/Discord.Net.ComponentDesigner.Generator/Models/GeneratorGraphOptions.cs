namespace ComponentDesigner;

public sealed record GeneratorGraphOptions(
    bool AllowAutoRows = false,
    bool AllowAutoTextDisplays = false,
    string? ProjectDirectory = null
) : IGraphOptions
{
    public static readonly GeneratorGraphOptions Default = new();

    public GeneratorGraphOptions WithOverloads(
        GraphOptionsOverloads overloads
    )
    {
        if (overloads.IsEmpty) return this;

        return this with
        {
            AllowAutoRows = overloads.EnableAutoRows.GetValueOrDefault(AllowAutoRows),
            AllowAutoTextDisplays = overloads.EnableAutoTextDisplays.GetValueOrDefault(AllowAutoTextDisplays)
        };
    }
}