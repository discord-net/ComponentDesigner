using ComponentDesigner.Nodes;
using ComponentDesigner.Parser;

namespace ComponentDesigner;

public sealed record GraphInitializationContext(
    CXDocument Document,
    ICXModel CX,
    IGraphOptions Options,
    IComponentImplementation Implementation,
    ICompilationProvider CompilationProvider,
    IDiagnosticBag Diagnostics
) : IComponentContext
{
    public CXComponentTree Tree { get; } = new();
    
    public bool Equals(IComponentContext? other)
        => other is GraphInitializationContext ctx && Equals(ctx);
}