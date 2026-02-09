using ComponentDesigner.Nodes;
using ComponentDesigner.Parser;

namespace ComponentDesigner;

public sealed record GraphInitializationContext(
    CXDocument Document,
    ICXModel CX,
    ICompilationProvider CompilationProvider,
    GraphOptions Options,
    IDiagnosticBag Diagnostics,
    IComponentRenderer Renderer
) : IGraphContext
{
    public bool Equals(IComponentContext? other)
        => other is GraphInitializationContext ctx && Equals(ctx);
}