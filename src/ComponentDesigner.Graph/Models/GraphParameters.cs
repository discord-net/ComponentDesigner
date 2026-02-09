namespace ComponentDesigner;

public sealed record GraphParameters(
    ICXModel CX,
    GraphOptions Options,
    ICompilationProvider CompilationProvider,
    IComponentRenderer Renderer
);