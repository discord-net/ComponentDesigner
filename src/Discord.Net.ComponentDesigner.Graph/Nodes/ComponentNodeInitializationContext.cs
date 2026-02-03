using Discord.CX.Parser;

namespace Discord.CX.Nodes;

public readonly struct ComponentNodeInitializationContext
{
    public ICXModel CX => GraphContext.CX;
    public ICompilationProvider CompilationProvider => GraphContext.CompilationProvider;

    public readonly GraphNode GraphNode;
    public readonly ICXNode? CXNode;
    public readonly IGraphContext GraphContext;

    public ComponentNodeInitializationContext(
        ICXNode? cxNode,
        GraphNode graphNode,
        IGraphContext context
    )
    {
        GraphNode = graphNode;
        CXNode = cxNode;
        GraphContext = context;
    }
}