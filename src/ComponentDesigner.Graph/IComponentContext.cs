namespace ComponentDesigner;

public interface IComponentContext
{
    IComponentImplementation Implementation { get; }
    ICompilationProvider CompilationProvider { get; }
    
    ICXModel CX { get; }
    
    IGraphOptions Options { get; }
}