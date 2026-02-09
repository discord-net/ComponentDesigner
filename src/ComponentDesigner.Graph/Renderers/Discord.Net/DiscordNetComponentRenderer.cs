using System.Collections.Immutable;
using System.Text;
using ComponentDesigner.CSharp;
using ComponentDesigner.Nodes;
using ComponentDesigner.Parser.Util;

namespace ComponentDesigner.Renderers.DiscordNet;

public sealed partial class DiscordNetComponentRenderer : BaseCSharpRenderer
{
    public override string Name => "Discord.Net";

    public override Result<string> RenderComponents(
        CXComponentGraph graph,
        ComponentEmitContext context,
        CancellationToken cancellationToken = default
    )
    {
        if (graph.RootNodes.Count is 0) return string.Empty;
        
        // TODO: assert top level node types
        return graph
            .RootNodes
            .Select(x => x.Emit(context, cancellationToken: cancellationToken))
            .FlattenAll()
            .Map(x => string.Join($",{Environment.NewLine}", x.Select(x => x.Source)));
    }


    private Result<EquatableArray<RenderedComponent>> RenderChildren(
        IRendererContext context,
        ComponentState state,
        CancellationToken token
    )
    {
        if (!state.HasGraphChildren) return EquatableArray<RenderedComponent>.Empty;

        return state
            .Children
            .Select(x => context.RenderGraphNode(x, cancellationToken: token))
            .FlattenAll();
    }

    private Result<EquatableArray<(ComponentProperty Property, string Render)>> RenderProperties(
        IRendererContext context,
        CancellationToken token,
        params IEnumerable<
            (ComponentPropertyValue PropertyValue, CSharpValueGenerator Generator)
        > properties
    )
    {
        using var _ = ObjectPool<List<(ComponentProperty, string)>>.GetScoped(out var result);
        result.Clear();

        var bag = DiagnosticBag.Get();

        foreach (var (propertyValue, generator) in properties)
        {
            if (ShouldOmitProperty(propertyValue)) continue;

            var render = generator.Render(context, propertyValue, default, token);

            if (render.HasValue) result.Add((propertyValue.Property, render.Value));

            bag.AddDiagnostics(render.Diagnostics);
        }

        return (
            new EquatableArray<(ComponentProperty, string)>(result.ToImmutableArray()),
            bag.Use()
        );
    }
    
    private static bool ShouldOmitProperty(ComponentPropertyValue propertyValue)
        => propertyValue.Property.IsSynthetic || (propertyValue.Property.IsOptional && !propertyValue.IsSpecified);
}