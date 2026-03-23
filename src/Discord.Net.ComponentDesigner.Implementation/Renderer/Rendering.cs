using System.Text;
using ComponentDesigner;
using ComponentDesigner.Nodes;
using ComponentDesigner.Util;

namespace Discord;

partial class DiscordNetRenderer
{
    private readonly struct PropertyRenderer
    {
        private readonly string? _value;
        private readonly CSharpValueGenerator? _generator;
        private readonly Func<IRendererContext, ComponentPropertyValue, CancellationToken, Result<string>>? _callback;

        private readonly Result<CSharpValueGenerator>? _generatorResult;

        public PropertyRenderer(Result<CSharpValueGenerator> generator)
        {
            _generatorResult = generator;
        }

        public PropertyRenderer(
            string value
        )
        {
            _value = value;
        }

        public PropertyRenderer(
            Func<IRendererContext, ComponentPropertyValue, CancellationToken, Result<string>> callback
        )
        {
            _callback = callback;
        }

        public PropertyRenderer(CSharpValueGenerator generator)
        {
            _generator = generator;
        }

        public Result<string> Render(
            IRendererContext context,
            ComponentPropertyValue value,
            CancellationToken cancellationToken
        )
        {
            if (_value is not null) return _value;

            if (_generator is not null) return _generator.Render(context, value, cancellationToken: cancellationToken);

            if (_callback is not null) return _callback(context, value, cancellationToken);

            if (_generatorResult is not null)
                return _generatorResult.Value
                    .Map(x => x.Render(context, value, cancellationToken: cancellationToken));

            return default;
        }

        public static implicit operator PropertyRenderer(CSharpValueGenerator generator)
            => new(generator);

        public static implicit operator PropertyRenderer(Result<CSharpValueGenerator> generator)
            => new(generator);

        public static implicit operator PropertyRenderer(
            Func<IRendererContext, ComponentPropertyValue, CancellationToken, Result<string>> callback
        ) => new(callback);
    }

    private delegate Result<string> GenericRenderer<T>(
        IRendererContext context,
        T value,
        CancellationToken cancellationToken
    ) where T : ComponentPropertyValue;

    private static Result<string> RenderGenericArrayOfValue(
        IRendererContext context,
        ComponentPropertyValue value,
        CancellationToken cancellationToken,
        GenericRenderer<ComponentPropertyValue.Literal>? literalHandler = null,
        GenericRenderer<ComponentPropertyValue.Component>? componentHandler = null,
        GenericRenderer<ComponentPropertyValue.Interpolation>? interpolationHandler = null,
        GenericRenderer<ComponentPropertyValue.None>? noneHandler = null
    )
    {
        var kind = ComponentPropertyValueKind.None;

        if (literalHandler is not null)
            kind |= ComponentPropertyValueKind.Literal;

        if (componentHandler is not null)
            kind |= ComponentPropertyValueKind.Component;

        if (interpolationHandler is not null)
            kind |= ComponentPropertyValueKind.Interpolation;

        using var bag = PooledDiagnosticBag.Get();
        using var _ = StringBuilder.Pooled(out var sb);

        foreach (var flattenedValue in value.AsFlattened)
        {
            var maybeResult = flattenedValue switch
            {
                ComponentPropertyValue.Literal literal when literalHandler is not null
                    => literalHandler(context, literal, cancellationToken),
                ComponentPropertyValue.Component component when componentHandler is not null
                    => componentHandler(context, component, cancellationToken),
                ComponentPropertyValue.Interpolation interpolation when interpolationHandler is not null
                    => interpolationHandler(context, interpolation, cancellationToken),
                ComponentPropertyValue.None none when noneHandler is not null
                    => noneHandler(context, none, cancellationToken),
                _ => (Result<string>?)null
            };

            if (!maybeResult.HasValue)
            {
                bag.Add(
                    Diagnostic
                        .InvalidPropertyValue(
                            flattenedValue,
                            kind
                        )
                        .At(flattenedValue)
                );
                
                continue;
            }

            var result = maybeResult.Value.Unwrap(bag);
            
            if(result is null) continue;

            if (sb.Length > 0) sb.AppendLine(",");

            sb.Append(result);
        }

        if (sb.Length is 0) return "[]";

        return
            $"""
             
             [
                 {sb.ToString().WithNewlinePadding(4)}
             ]
             """;
    }

    private static Result<string> RenderAsSingleChildComponent(
        IRendererContext context,
        ComponentPropertyValue value,
        CancellationToken cancellationToken
    )
    {
        if (value.AsSingle is not ComponentPropertyValue.Component { GraphNode: var graphNode })
        {
            return Diagnostic
                .InvalidPropertyValue(
                    value,
                    ComponentPropertyValueKind.Component
                )
                .At(value);
        }

        return context
            .RenderGraphNode(
                graphNode,
                cancellationToken: cancellationToken
            )
            .Map(x => x.Source);
    }

    private static Result<string> RenderAsChildComponents(
        IRendererContext context,
        ComponentPropertyValue value,
        ICSharpTypeSymbol targetType,
        CancellationToken cancellationToken
    ) => RenderAsChildComponents(context, value, targetType, cancellationToken, true);

    private static Result<string> RenderAsChildComponents(
        IRendererContext context,
        ComponentPropertyValue value,
        ICSharpTypeSymbol targetType,
        CancellationToken cancellationToken,
        bool withinCollectionExpression
    )
    {
        if (value.IsNone) return withinCollectionExpression ? "[]" : string.Empty;

        var sb = new StringBuilder();
        using var bag = PooledDiagnosticBag.Get();

        var options = new ComponentOptions(
            new RendererTypingContext(targetType)
        );

        foreach (var childValue in value.AsFlattened)
        {
            if (childValue is not ComponentPropertyValue.Component { GraphNode: var child })
            {
                bag.Add(
                    Diagnostic
                        .InvalidPropertyValue(
                            childValue,
                            ComponentPropertyValueKind.Component
                        )
                        .At(childValue)
                );

                continue;
            }

            var result = context.RenderGraphNode(
                child,
                options,
                cancellationToken
            );

            bag.Add(result.Diagnostics);

            if (!result.HasValue) continue;

            if (sb.Length > 0) sb.AppendLine(",");

            var source = result.Value.Source;

            if (withinCollectionExpression)
            {
                sb.Append("    ");
                source = source.WithNewlinePadding(4);
            }

            sb.Append(source);
        }

        if (sb.Length is 0)
            return new(withinCollectionExpression ? "[]" : string.Empty, bag.ToCollection());

        if (withinCollectionExpression)
            sb.Insert(0, Environment.NewLine).AppendLine();

        return new(
            withinCollectionExpression ? $"{Environment.NewLine}[{sb}]" : sb.ToString(),
            bag.ToCollection()
        );
    }

    private static Result<string> RenderPropertiesAsParameters(
        IRendererContext context,
        ComponentState state,
        CancellationToken cancellationToken,
        params IEnumerable<(string Name, ComponentProperty Property, PropertyRenderer Renderer)> properties
    ) => RenderPropertiesAsParameters(context, state, cancellationToken, explicitParameters: null, properties);

    private static Result<string> RenderPropertiesAsParameters(
        IRendererContext context,
        ComponentState state,
        CancellationToken cancellationToken,
        IEnumerable<(string Name, string Value)>? explicitParameters = null,
        params IEnumerable<(string Name, ComponentProperty Property, PropertyRenderer Renderer)> properties
    )
    {
        var sb = new StringBuilder();
        using var bag = PooledDiagnosticBag.Get();

        if (explicitParameters is not null)
        {
            foreach (var (name, value) in explicitParameters)
            {
                AppendProperty(sb, name, value);
            }
        }

        foreach (var (name, property, generator) in properties)
        {
            var value = state.GetPropertyValue(property);

            if (ShouldOmit(value)) continue;

            var render = generator.Render(context, value, cancellationToken);

            bag.Add(render.Diagnostics);

            if (!render.HasValue) continue;

            AppendProperty(sb, name, render.Value);
        }

        if (bag.HasErrors) return new(bag.ToCollection());

        if (sb.Length > 0)
            sb.Insert(0, Environment.NewLine).AppendLine();

        return new(sb.ToString(), bag.ToCollection());

        static void AppendProperty(StringBuilder builder, string name, string value)
        {
            if (builder.Length > 0) builder.AppendLine(",");

            builder.Append("    ").Append(name).Append(": ").Append(value.WithNewlinePadding(4));
        }
    }


    private static bool ShouldOmit(ComponentPropertyValue propertyValue)
        => propertyValue.Property.IsSynthetic || propertyValue is { Property.IsOptional: true, IsNone: true, IsAttributeNameOnly: false };
}