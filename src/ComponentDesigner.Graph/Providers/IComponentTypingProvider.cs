using ComponentDesigner.Nodes;

namespace ComponentDesigner;

public interface IComponentTypingProvider
{
    bool IsValidComponentType(
        IComponentContext context, 
        ICSharpTypeSymbol? symbol, 
        CancellationToken cancellationToken = default
    );
}