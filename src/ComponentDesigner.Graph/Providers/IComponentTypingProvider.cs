using ComponentDesigner.Nodes;

namespace ComponentDesigner;

public interface IComponentTypingProvider
{
    bool IsValidComponentType(
        IComponentContext context, 
        ICSharpTypeSymbol? symbol, 
        CancellationToken cancellationToken = default
    );

    Result<string> Convert(
        IComponentContext context,
        SourcedValue<string> source,
        ICSharpTypeSymbol from,
        ICSharpTypeSymbol to,
        CancellationToken cancellationToken = default
    );
}