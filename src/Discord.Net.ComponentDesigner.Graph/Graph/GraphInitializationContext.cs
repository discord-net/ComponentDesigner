using Discord.CX.Nodes;
using Discord.CX.Parser;

namespace Discord.CX;

public sealed record GraphInitializationContext(
    CXDocument Document,
    ICXModel CX,
    ICompilationProvider CompilationProvider,
    GraphOptions Options,
    IList<Diagnostic> Diagnostics,
    IComponentRenderer Renderer
) : IGraphContext
{
    public bool Equals(IComponentContext? other)
        => other is GraphInitializationContext ctx && Equals(ctx);
}