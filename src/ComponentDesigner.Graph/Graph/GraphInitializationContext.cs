using ComponentDesigner.Nodes;
using ComponentDesigner.Parser;

namespace ComponentDesigner;

public sealed record GraphInitializationContext(
    CXDocument Document,
    ICXModel CX,
    GraphOptions Options,
    IComponentImplementation Implementation,
    IDiagnosticBag Diagnostics
) : IComponentContext
{
    public bool Equals(IComponentContext? other)
        => other is GraphInitializationContext ctx && Equals(ctx);
}