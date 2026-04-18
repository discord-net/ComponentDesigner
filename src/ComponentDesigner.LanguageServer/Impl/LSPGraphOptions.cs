using ComponentDesigner;
using ComponentDesigner.Nodes;

namespace Discord.ComponentDesigner.LanguageServer;

public sealed record LSPGraphOptions(
    bool AllowAutoRows,
    bool AllowAutoTextDisplays,
    ComponentTargetType Target = ComponentTargetType.Any
) : IGraphOptions;