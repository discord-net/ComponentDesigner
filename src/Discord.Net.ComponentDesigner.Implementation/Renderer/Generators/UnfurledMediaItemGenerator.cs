using ComponentDesigner;
using ComponentDesigner.CSharp;
using ComponentDesigner.Nodes;
using ComponentDesigner.Util;

namespace Discord;

public sealed class UnfurledMediaItemGenerator : CSharpValueGenerator
{
    public static readonly UnfurledMediaItemGenerator Instance = new();

    public override Result<CSharpRender> Render(
        IRenderContext context,
        ComponentPropertyValue value,
        CancellationToken cancellationToken = default
    ) => String
        .Render(context, value, cancellationToken)
        .Combine(
            context.CompilationProvider.UnfurledMediaItemProperties(value, cancellationToken),
            (render, symbol) => render with
            {
                Symbol = symbol,
                Source =
                $"""
                 new {symbol.ToQualifiedName()}(
                     {render.Source.TrimStart().WithNewlinePadding(4)}
                 )
                 """
            }
        );
}