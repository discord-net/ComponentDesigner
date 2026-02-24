namespace ComponentDesigner;

public interface IComponentContext : IEquatable<IComponentContext>
{
    IComponentImplementation Implementation { get; }
    ICompilationProvider CompilationProvider { get; }
    
    ICXModel CX { get; }
    
    IGraphOptions Options { get; }
}