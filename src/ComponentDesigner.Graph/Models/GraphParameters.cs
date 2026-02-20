namespace ComponentDesigner;

public sealed record GraphParameters(
    IComponentImplementation Implementation,
    ICXModel CX,
    GraphOptions Options
);