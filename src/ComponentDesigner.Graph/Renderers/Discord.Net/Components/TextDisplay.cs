using System.Text;
using ComponentDesigner.Nodes;

namespace ComponentDesigner.Renderers.DiscordNet;

partial class DiscordNetComponentRenderer
{
    public override Result<RenderedComponent> RenderTextDisplay(
        IRendererContext context,
        TextDisplayComponentNode textDisplay,
        TextDisplayState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    )
    {
        var id = (
            state.GetPropertyValue(textDisplay.Id),
            IntegerGenerator.Get(allowNullable: true)
        );

        (ComponentPropertyValue, CSharpValueGenerator)[] properties = state.Content is not null
            ?
            [
                id,
                (state.GetPropertyValue(textDisplay.Content), StringGenerator.Get(StringNullMode.DisallowNull))
            ]
            : [id];

        return RenderProperties(
                context,
                cancellationToken,
                properties
            )
            .Combine(
                state.Content?.RenderToCSharpString(context, cancellationToken) ?? string.Empty,
                static (properties, content) =>
                {
                    using var _ = ObjectPool<StringBuilder>.GetScoped(out var sb);
                    sb.Clear();

                    for (var i = 0; i < properties.Count; i++)
                    {
                        if (i > 0)
                            sb.AppendLine(",");

                        var (property, render) = properties[i];
                        sb.Append("    ")
                            .Append(GetPropertyParameterName(property))
                            .Append(": ")
                            .Append(render);
                    }

                    if (string.IsNullOrEmpty(content))
                    {
                        if (sb.Length > 0) sb.AppendLine(",    ");
                        sb.Append("content: ").Append(content);
                    }

                    if (sb.Length > 0)
                    {
                        sb.Insert(0, Environment.NewLine).AppendLine();
                    }

                    return sb.ToString();
                }
            )
            .Combine(
                TextDisplayBuilder(state.TextSpan, context.CompilationProvider),
                (parameters, symbol) => new RenderedComponent(
                    $"new {symbol.ToQualifiedName()}({parameters})",
                    symbol
                )
            );

        static string GetPropertyParameterName(ComponentProperty property)
            => property.Name switch
            {
                "id" => "id",
                "content" => "content",
                _ => throw new InvalidOperationException(
                    $"The property '{property.Name}' isn't a known property of the text-display builder"
                )
            };
    }
}