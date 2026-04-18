using ComponentDesigner.Nodes;

namespace ComponentDesigner;

public sealed record GeneratorGraphOptions(
    bool AllowAutoRows = false,
    bool AllowAutoTextDisplays = false,
    string? ProjectDirectory = null,
    ComponentTargetType Target = ComponentTargetType.Any
) : IGraphOptions
{
    public static readonly GeneratorGraphOptions Default = new();

    public GeneratorGraphOptions WithOverloads(
        GraphOptionsOverloads overloads,
        ComponentTargetType? target = null
    ) => this with
    {
        AllowAutoRows = overloads.EnableAutoRows.GetValueOrDefault(AllowAutoRows),
        AllowAutoTextDisplays = overloads.EnableAutoTextDisplays.GetValueOrDefault(AllowAutoTextDisplays),
        Target = target ?? Target
    };
}