namespace ComponentDesigner;

public interface IComponentImplementation
{
    string Name { get; }
    
    ICompilationProvider CompilationProvider { get; }
    
    IComponentRenderer Renderer { get; }
    
    ITextControlProvider TextControlProvider { get; }
    
    IComponentTypingProvider ComponentTypingProvider { get; }
}