using System.Text;
using ComponentDesigner;
using ComponentDesigner.CSharp;
using ComponentDesigner.Nodes;

namespace Discord;

public sealed partial class DiscordNetRenderer : BaseCSharpRenderer
{
    public override string Name => "Discord.Net";
    
    public override Result<string> RenderComponents(
        CXComponentGraph graph,
        ComponentEmitContext context,
        CancellationToken cancellationToken = default
    )
    {
        var sb = new StringBuilder();
        using var bag = PooledDiagnosticBag.Get(); 
        
        foreach (var node in graph.RootNodes)
        {
            var render = node.Emit(context, cancellationToken: cancellationToken);
            
            bag.Add(render.Diagnostics);
            if(!render.HasValue) continue;

            if (sb.Length > 0) sb.AppendLine(",");

            sb.Append(render.Value.Source);
        }

        if (bag.HasErrors) return new(bag.ToCollection());

        return new(sb.ToString(), bag.ToCollection());
    }
}