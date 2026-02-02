using Discord.CX.Nodes;

namespace Discord.CX;

public interface IComponentRenderer
{
    string Name { get; }
    
    Result<string> Render(
        IRendererContext context,
        IComponentNode component,
        ComponentState state,
        CancellationToken token = default
    );
}