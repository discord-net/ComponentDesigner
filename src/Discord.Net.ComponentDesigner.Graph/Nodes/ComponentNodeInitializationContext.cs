using Discord.CX.Parser;

namespace Discord.CX.Nodes;

public readonly struct ComponentNodeInitializationContext
{
    public ICXModel CX => ComponentContext.CX;
    public ICompilationProvider CompilationProvider => ComponentContext.CompilationProvider;

    public readonly GraphNode GraphNode;
    public readonly ICXNode? CXNode;
    public readonly IComponentContext ComponentContext;

    public ComponentNodeInitializationContext(
        ICXNode? cxNode,
        GraphNode graphNode,
        IComponentContext context
    )
    {
        GraphNode = graphNode;
        CXNode = cxNode;
        ComponentContext = context;
    }
}