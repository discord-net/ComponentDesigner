using ComponentDesigner.Nodes;
using ComponentDesigner.Nodes.TextControls;

namespace ComponentDesigner;

public interface IComponentRenderer<TRender>
{
    Result<TRender> RenderComponent(
        IRenderContext<TRender> context,
        IComponentNode component,
        ComponentState state,
        CancellationToken cancellationToken = default
    );
}

public interface IComponentRenderer<TGraph, TRender> : IComponentRenderer<TRender>
{
    Result<TGraph> RenderGraph(
        IRenderContext<TRender> context,
        CXComponentGraph graph,
        CancellationToken cancellationToken = default
    );
}