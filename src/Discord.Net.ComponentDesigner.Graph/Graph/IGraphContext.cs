using Discord.CX.Nodes;

namespace Discord.CX;

public interface IGraphContext : IComponentContext
{
    IComponentRenderer Renderer { get; }
}