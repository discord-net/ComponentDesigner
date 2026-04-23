using ComponentDesigner.Nodes;

namespace ComponentDesigner;

public interface IRenderContext : IComponentContext
{
    CXComponentGraph Graph { get; }
}

public interface IRenderContext<TRender> : IRenderContext
{
    IComponentRenderer<TRender> Renderer { get; }
}