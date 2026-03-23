using ComponentDesigner;
using ComponentDesigner.Nodes;

namespace Discord;

public sealed class UnfurledMediaItemGenerator : CSharpValueGenerator
{
    public static readonly UnfurledMediaItemGenerator Instance = new();

    public override Result<string> Render(
        IRendererContext context,
        ComponentPropertyValue value,
        CancellationToken cancellationToken = default
    ) => String
        .Render(context, value, cancellationToken)
        .Combine(
            context.CompilationProvider.UnfurledMediaItemProperties(value, cancellationToken),
            (url, symbol) => 
                $"""
                new {symbol.ToQualifiedName()}(
                    {url}
                )
                """
        );
}