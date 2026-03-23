using ComponentDesigner.Nodes;
using ComponentDesigner.Util;

namespace ComponentDesigner;

public interface IComponentImplementation
{
    string Name { get; }
    
    IComponentRenderer Renderer { get; }
    
    ITextControlProvider TextControlProvider { get; }
    
    IComponentTypingProvider? ComponentTypingProvider { get; }

    ComponentPropertyValueKind? GetPropertyKindOverload(
        IComponentNode component,
        ComponentProperty property
    );

    bool TryAnalyzeNumberOfValues(
        IComponentContext context,
        IComponentNode component,
        ComponentPropertyValue propertyValue,
        CancellationToken cancellationToken,
        out StaticRange range
    );
}