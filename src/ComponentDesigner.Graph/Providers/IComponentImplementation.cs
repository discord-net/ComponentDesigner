namespace ComponentDesigner;

public interface IComponentImplementation
{
    string Name { get; }
    
    IComponentRenderer Renderer { get; }
    
    ITextControlProvider TextControlProvider { get; }
    
    IComponentTypingProvider? ComponentTypingProvider { get; }
}