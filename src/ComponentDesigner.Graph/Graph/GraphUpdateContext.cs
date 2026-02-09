using ComponentDesigner.Nodes;

namespace ComponentDesigner;

public sealed record GraphUpdateContext(
    ICompilationProvider CompilationProvider,
    ICXModel CX,
    GraphOptions Options,
    IComponentRenderer Renderer
) : IGraphContext
{
    public bool Equals(IComponentContext? obj) => obj is GraphUpdateContext other && Equals(other);
}