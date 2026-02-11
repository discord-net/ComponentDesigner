using System.Text;
using ComponentDesigner;
using ComponentDesigner.Nodes;
using ComponentDesigner.Util;

namespace Discord.ComponentDesigner;

partial class DiscordNetRenderer
{
    private readonly struct PropertyRenderer
    {
        private readonly CSharpValueGenerator? _generator;
        private readonly Func<IRendererContext, ComponentPropertyValue, CancellationToken, Result<string>>? _callback;

        public PropertyRenderer(
            Func<IRendererContext, ComponentPropertyValue, CancellationToken, Result<string>> callback)
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
            if (_generator is not null) return _generator.Render(context, value, cancellationToken: cancellationToken);

            if (_callback is not null) return _callback(context, value, cancellationToken);

            return default;
        }

        public static implicit operator PropertyRenderer(CSharpValueGenerator generator)
            => new(generator);
        public static implicit operator PropertyRenderer(
            Func<IRendererContext, ComponentPropertyValue, CancellationToken, Result<string>> callback
            ) => new(callback);
    }

    private static Result<string> RenderAsSingleChildComponent(
        IRendererContext context,
        ComponentPropertyValue value,
        CancellationToken cancellationToken
    )
    {
        if (value is not ComponentPropertyValue.Children { GraphNodes: {Count: 1} children }) return default;

        return context.RenderGraphNode(
            children[0],
            cancellationToken: cancellationToken
        ).Map(x => x.Source);
    }
    
    private static Result<string> RenderAsChildComponents(
        IRendererContext context,
        ComponentPropertyValue value,
        CancellationToken cancellationToken
    )
    {
        if (value is not ComponentPropertyValue.Children { GraphNodes: {} children }) return default;

        if (children.Count is 0) return "[]";

        var sb = new StringBuilder();
        using var bag = PooledDiagnosticBag.Get();
        
        foreach (var child in children)
        {
            var result = context.RenderGraphNode(
                child,
                cancellationToken: cancellationToken
            );
            
            bag.Add(result.Diagnostics);
            
            if(!result.HasValue) continue;

            if (sb.Length > 0) sb.AppendLine(",");

            sb.Append("    ").Append(result.Value.Source.WithNewlinePadding(4));
        }

        if (sb.Length is 0) return new("[]", bag.ToCollection());

        sb.Insert(0, Environment.NewLine).AppendLine();

        return new($"[{sb}]", bag.ToCollection());
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
        => propertyValue.Property.IsSynthetic || propertyValue is { IsOptional: true, IsSpecified: false };
}