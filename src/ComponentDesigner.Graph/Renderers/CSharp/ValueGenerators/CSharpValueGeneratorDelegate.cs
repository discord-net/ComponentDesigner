using ComponentDesigner.Nodes;

namespace ComponentDesigner;

public delegate Result<string> CSharpValueGeneratorDelegate(
    IComponentContext context,
    CSharpValueGeneratorTarget target,
    CSharpValueGeneratorOptions options
);