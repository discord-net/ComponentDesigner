using ComponentDesigner;

namespace Discord.ComponentDesigner.LanguageServer;

public sealed record class LSPGraphOptions(bool AllowAutoRows, bool AllowAutoTextDisplays) : IGraphOptions;