using Discord.CX.Nodes;

namespace Discord.CX;

public delegate Result<string> CSharpValueGeneratorDelegate(
    IComponentContext context,
    CSharpValueGeneratorTarget target,
    CSharpValueGeneratorOptions options
);