using ComponentDesigner.Nodes;
using ComponentDesigner.Util;

namespace ComponentDesigner;

public interface IComponentImplementation
{
    string Name { get; }
    
    ITextControlProvider TextControlProvider { get; }
    
    IComponentTypingProvider? ComponentTypingProvider { get; }

    IComponentExtensionProvider? ComponentExtensionProvider { get; }

    bool TryAnalyzeNumberOfValues(
        IComponentContext context,
        IComponentNode component,
        ComponentPropertyValue propertyValue,
        CancellationToken cancellationToken,
        out StaticRange range
    );
}