namespace ComponentDesigner.Nodes;

public sealed record ComponentValidationContext(
    CXComponentGraph Graph,
    ICompilationProvider CompilationProvider
) : IComponentContext
{
    public bool Equals(IComponentContext obj)
        => obj is ComponentValidationContext other && Equals(other);

    public IComponentImplementation Implementation => Graph.Implementation;

    public ICXModel CX => Graph.CX;

    public IGraphOptions Options => Graph.Options;
}