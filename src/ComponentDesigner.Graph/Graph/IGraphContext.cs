using ComponentDesigner.Nodes;
using ComponentDesigner.Parser;

namespace ComponentDesigner;

public interface IGraphContext : IComponentContext
{
    GraphNode? Push(
        GraphNodeInitializationRequest request,
        CancellationToken cancellationToken = default
    );

    IReadOnlyList<GraphNode> Push(
        GraphNode? parent,
        IReadOnlyList<ICXNode> syntaxNodes,
        CancellationToken cancellationToken
    );
}

public static class GraphContextExtensions
{
    extension(IGraphContext context)
    {
        
    }
}