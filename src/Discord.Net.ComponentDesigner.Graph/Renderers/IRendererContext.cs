using Discord.CX.Nodes;

namespace Discord.CX;

public interface IRendererContext : IComponentContext
{
    string CreateVariable(string hint = "local_");

    Result<string> Render(GraphNode node, ComponentOptions options = default, CancellationToken token = default);
}