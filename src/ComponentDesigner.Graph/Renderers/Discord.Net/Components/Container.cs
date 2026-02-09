using System.Text;
using ComponentDesigner.Nodes;
using ComponentDesigner.Parser.Util;
using ComponentDesigner.Util;

namespace ComponentDesigner.Renderers.DiscordNet;

partial class DiscordNetComponentRenderer
{
    public const string CONTAINER_TYPE_NAME = "global::Discord.ContainerBuilder";
    
    public override Result<RenderedComponent> RenderContainer(
        IRendererContext context,
        ContainerComponentNode component,
        ComponentState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    )
    {
        var id = state.GetPropertyValue(component.Id);
        var accentColor = state.GetPropertyValue(component.AccentColor);
        var isSpoiler = state.GetPropertyValue(component.IsSpoiler);

        var properties = RenderProperties(
            context,
            cancellationToken,
            (id, IntegerGenerator.Get(allowNullable: true)),
            (accentColor, ColorGenerator.Get(allowNullable: true)),
            (isSpoiler, BooleanGenerator.Get(allowNullable: true))
        );

        var children = RenderChildren(context, state, cancellationToken);

        return children
            .Combine(properties, (children, properties) =>
            {
                using var _ = ObjectPool<StringBuilder>.GetScoped(out var initializers);
                initializers.Clear();

                for (var i = 0; i < properties.Count; i++)
                {
                    var (property, render) = properties[i];

                    if (i > 0) initializers.AppendLine();

                    initializers.Append($"    {ToBuilderPropertyName(property)} = {render.WithNewlinePadding(4)}");
                }

                if (children.Count > 0)
                {
                    if (properties.Count > 0) initializers.AppendLine();

                    initializers.Append(
                        $"""
                             Components =
                             [
                                 {string.Join(Environment.NewLine, children).WithNewlinePadding(8)}
                             ]
                         """
                    );
                }

                if (initializers.Length > 0)
                {
                    initializers.Insert(0, $"{Environment.NewLine}{{");
                    initializers.Append($"{Environment.NewLine}}}");
                }

                return initializers.ToString();
            })
            .Combine(
                ContainerBuilder(state.TextSpan, context.CompilationProvider),
                (initializers, symbol) => new RenderedComponent(
                    $"new {symbol.ToQualifiedName()}(){initializers}",
                    symbol
                )
            );


        static string ToBuilderPropertyName(ComponentProperty property)
            => property.Name switch
            {
                "id" => "Id",
                "accentColor" => "AccentColor",
                "spoiler" => "IsSpoiler",
                _ => throw new InvalidOperationException(
                    $"The property '{property.Name}' isn't a known property of the container builder")
            };
    }
}