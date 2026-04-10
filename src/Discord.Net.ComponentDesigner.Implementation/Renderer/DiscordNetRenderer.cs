using System.Text;
using ComponentDesigner;
using ComponentDesigner.CSharp;
using ComponentDesigner.Nodes;

namespace Discord;

public sealed partial class DiscordNetRenderer : BaseCSharpRenderer
{
    public Func<RenderedComponent, Result<RenderedComponent>> ApplyRefParameter(
        IRendererContext context,
        ComponentState state,
        CancellationToken cancellationToken
    )
    {
        if (!state.PropertyInfo.TryGet("ref", out var property))
            return x => x;

        var value = state.GetPropertyValue(property);

        if (value.AsSingle is not ComponentPropertyValue.Interpolation interpolation)
            return x => x;

        var refParameterSymbol = interpolation.Info.Symbol;

        return render =>
        {
            return context.CompilationProvider
                .RefBox(state, cancellationToken)
                .Map(Result<RenderedComponent> (refBox) =>
                {
                    if (
                        refParameterSymbol?.ConstructedFrom is null ||
                        !refBox.Equals(refParameterSymbol.ConstructedFrom)
                    )
                    {
                        return Diagnostic
                            .TypeMismatch(
                                refBox,
                                refParameterSymbol?.ConstructedFrom
                            )
                            .At(value);
                    }

                    if (render.Type is not null)
                    {
                        var inner = refParameterSymbol.TypeArguments[0];
                        if (!context.CompilationProvider.HasImplicitConversionBetween(render.Type, inner))
                        {
                            return Diagnostic
                                .TypeMismatch(
                                    render.Type,
                                    inner
                                )
                                .At(value);
                        }
                    }

                    return render with
                    {
                        Source =
                        $"""
                         {context.GetReferenceToDesignerValue(interpolation.Info, interpolation.Info.Symbol)}.Set(
                             {render.Source}
                         )
                         """
                    };
                });
        };
    }

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
            var render = node.Render(context, cancellationToken: cancellationToken);

            bag.Add(render.Diagnostics);
            if (!render.HasValue) continue;

            if (sb.Length > 0) sb.AppendLine(",");

            sb.Append(render.Value.Source);
        }

        if (bag.HasErrors) return new(bag.ToCollection());

        return new(sb.ToString(), bag.ToCollection());
    }
}