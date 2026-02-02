using System.Collections.Immutable;
using System.Text;
using Discord.CX.Nodes;
using Discord.CX.Util;

namespace Discord.CX.Renderers.DiscordNet;

public sealed class DiscordNetComponentRenderer : BaseComponentRenderer
{
    public const string CONTAINER_TYPE_NAME = "global::Discord.ContainerBuilder";

    public override string Name => "Discord.Net";

    protected override Result<string> Container(
        IRendererContext context,
        ContainerComponentNode component,
        ComponentState state,
        ComponentPropertyValue id,
        ComponentPropertyValue accentColor,
        ComponentPropertyValue isSpoiler,
        CancellationToken token = default
    )
    {
        var properties = RenderProperties(
            context,
            token,
            (id, IntegerGenerator.Get(allowNullable: true)),
            (accentColor, ColorGenerator.Get(allowNullable: true)),
            (isSpoiler, BooleanGenerator.Get(allowNullable: true))
        );

        var children = RenderChildren(context, state, token);

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

                return $"new {CONTAINER_TYPE_NAME}(){initializers}";
            });

        static string ToBuilderPropertyName(ComponentProperty property)
            => property.Name switch
            {
                "id" => "Id",
                "accentColor" => "AccentColor",
                "spoiler" => "IsSpoiler",
                _ => throw new InvalidOperationException($"The property '{property.Name}' isn't a known property of the container builder")
            };
    }

    private Result<EquatableArray<string>> RenderChildren(
        IRendererContext context,
        ComponentState state,
        CancellationToken token
    )
    {
        if (!state.HasGraphChildren) return EquatableArray<string>.Empty;

        return state
            .Children
            .Select(x => context.Render(x, token: token))
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
            if(ShouldOmit(propertyValue)) continue;

            var render = generator.Render(context, propertyValue, default, token);
            
            if(render.HasValue) result.Add((propertyValue.Property, render.Value));
            
            bag.AddDiagnostics(render.Diagnostics);
        }

        return (
            new EquatableArray<(ComponentProperty, string)>(result.ToImmutableArray()),
            bag.Use()
        );

        static bool ShouldOmit(ComponentPropertyValue propertyValue)
            => propertyValue.Property.IsSynthetic || (propertyValue.Property.IsOptional && !propertyValue.IsSpecified);
    }
}