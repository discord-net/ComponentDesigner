namespace ComponentDesigner;

public interface IComponentContext : IEquatable<IComponentContext>
{
    IComponentImplementation Implementation { get; }
    
    ICXModel CX { get; }
    
    GraphOptions Options { get; }
}