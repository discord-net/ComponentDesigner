namespace ComponentDesigner;

public sealed record GraphParameters(
    IComponentImplementation Implementation,
    ICompilationProvider CompilationProvider,
    ICXModel CX,
    GraphOptions Options
);