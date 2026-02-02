using Discord.CX.Nodes;
using Discord.CX.Parser;

namespace Discord.CX;

public sealed record GraphInitializationContext(
    CXDocument Document,
    ICXModel CX,
    ICompilationProvider CompilationProvider,
    GraphOptions Options,
    IList<Diagnostic> Diagnostics
) : IComponentContext
{
    public bool Equals(IComponentContext? other)
        => other is GraphInitializationContext ctx && Equals(ctx);
}