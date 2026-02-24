namespace ComponentDesigner;

public sealed record GraphParameters(
    IComponentImplementation Implementation,
    ICompilationProvider CompilationProvider,
    ICXModel CX,
    IGraphOptions Options
);