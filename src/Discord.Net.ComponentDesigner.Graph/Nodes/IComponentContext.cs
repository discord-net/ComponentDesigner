namespace Discord.CX.Nodes;

public interface IComponentContext : IEquatable<IComponentContext>
{
    ICompilationProvider CompilationProvider { get; }
    
    ICXModel CX { get; }
    
    GraphOptions Options { get; }
}