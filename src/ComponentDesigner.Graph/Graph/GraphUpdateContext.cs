using ComponentDesigner.Nodes;

namespace ComponentDesigner;

public sealed record GraphUpdateContext(
    ICXModel CX,
    GraphOptions Options,
    IComponentImplementation Implementation,
    ICompilationProvider CompilationProvider
) : IComponentContext
{
    public bool Equals(IComponentContext? obj) => obj is GraphUpdateContext other && Equals(other);
}