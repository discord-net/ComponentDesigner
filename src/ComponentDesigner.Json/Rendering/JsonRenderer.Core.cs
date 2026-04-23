using System;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using ComponentDesigner.Nodes;

namespace ComponentDesigner.Json;

partial class JsonRenderer
{
    private delegate Result<JsonNode> PropertyRenderer(
        IRenderContext<JsonNode> context,
        ComponentPropertyValue propertyValue,
        CancellationToken cancellationToken
    );

    private readonly record struct PropertySpecRenderer(object Value)
    {
        public Result<JsonNode> GetJsonNode(
            IRenderContext<JsonNode> context,
            ComponentPropertyValue propertyValue,
            CancellationToken cancellationToken
        ) => Value switch
        {
            JsonNode node => node,
            PropertyRenderer renderer => renderer(context, propertyValue, cancellationToken),
            _ => throw new InvalidOperationException("Empty property spec")
        };

        public static implicit operator PropertySpecRenderer(PropertyRenderer renderer)
            => new(renderer);
        
        public static implicit operator PropertySpecRenderer(JsonNode node)
            => new(node);
    }

    private readonly struct PropertySpec
    {
        public readonly string JsonName;
        public readonly ComponentProperty? Property;
        public readonly PropertySpecRenderer Renderer;

        private PropertySpec(string name, ComponentProperty? property, PropertySpecRenderer renderer)
        {
            JsonName = name;
            Property = property;
            Renderer = renderer;
        }

        public static implicit operator PropertySpec(
            (string, ComponentProperty, PropertyRenderer) tuple
        ) => new(tuple.Item1, tuple.Item2, new(tuple.Item3));
        
        public static implicit operator PropertySpec(
            (string, ComponentProperty, PropertySpecRenderer) tuple
        ) => new(tuple.Item1, tuple.Item2, new(tuple.Item3));

        public static implicit operator PropertySpec(
            (string, ComponentProperty, string) tuple
        ) => new(tuple.Item1, tuple.Item2, new(JsonValue.Create(tuple.Item3)));

        public static implicit operator PropertySpec(
            (string, ComponentProperty, int) tuple
        ) => new(tuple.Item1, tuple.Item2, new(JsonValue.Create(tuple.Item3)));
        
        public static implicit operator PropertySpec(
            (string, JsonNode) tuple
        ) => new(tuple.Item1, null, new(tuple.Item2));

        public static implicit operator PropertySpec(
            (string, string) tuple
        ) => new(tuple.Item1, null, new(JsonValue.Create(tuple.Item2)));

        public static implicit operator PropertySpec(
            (string, int) tuple
        ) => new(tuple.Item1, null, new(JsonValue.Create(tuple.Item2)));
    }

    private static readonly PropertyRenderer UnfurledMediaItem = static (context, propertyValue, cancellationToken) =>
        PropertyTransformer.String(context, propertyValue, cancellationToken).Map(JsonNode (url) => new JsonObject()
        {
            ["url"] = url
        });
    private static readonly PropertySpecRenderer Bool = (PropertyRenderer)RenderBool;
    private static readonly PropertySpecRenderer String = (PropertyRenderer)RenderString;
    private static readonly PropertySpecRenderer Number = (PropertyRenderer)RenderNumber;
    private static readonly PropertySpecRenderer Color = Transformed(PropertyTransformer.ColorCode, JsonValue.Create);
    private static readonly PropertySpecRenderer Emoji = Transformed(PropertyTransformer.PartialEmoji, JsonNode (emoji) =>
        emoji switch
        {
            PartialEmoji.GuildEmote guildEmote => new JsonObject()
            {
                ["id"] = guildEmote.Id,
                ["name"] = guildEmote.Name,
                ["animated"] = guildEmote.IsAnimated
            },
            PartialEmoji.Unicode unicode => new JsonObject()
            {
                ["id"] = null,
                ["name"] = unicode.Value
            },
            _ => throw new ArgumentOutOfRangeException(nameof(emoji))
        });
    private static readonly PropertySpecRenderer Component = (PropertyRenderer)RenderSingleComponent;
    private static readonly PropertySpecRenderer ComponentArray = (PropertyRenderer)RenderComponentArray;

    private static PropertyRenderer Enum(
        params (string Name, int Value)[] members
    ) => (PropertyRenderer)((context, propertyValue, cancellationToken) =>
    {
        if (propertyValue.AsSingle is not ComponentPropertyValue.Literal literal)
            return Diagnostic.TypeMismatch("number | enum", propertyValue.Kind.ReadableName).At(propertyValue);

        if (int.TryParse(literal.Value, out var num))
        {
            for (var i = 0; i < members.Length; i++)
                if (members[i].Value == num)
                    return JsonValue.Create(num);

            return Diagnostic
                .NotAValidEnumValue(num)
                .At(propertyValue);
        }

        for (var i = 0; i < members.Length; i++)
            if (members[i].Name.Equals(literal.Value, StringComparison.InvariantCultureIgnoreCase))
                return JsonValue.Create(members[i].Value);

        return Diagnostic
            .NotAValidEnumValue(literal.Value)
            .At(propertyValue);
    });

    private static Result<JsonNode> Spec(
        IRenderContext<JsonNode> context,
        ComponentState state,
        CancellationToken cancellationToken,
        params ReadOnlySpan<PropertySpec> properties
    )
    {
        var obj = new JsonObject();
        using var builder = Result<JsonNode>.Builder.WithValue(obj);

        for (var i = 0; i < properties.Length; i++)
        {
            var spec = properties[i];

            JsonNode? value;

            if (spec.Property is null)
            {
                if (spec.Renderer.Value is not JsonNode render)
                    throw new InvalidOperationException("Synthetic properties require a constant value");

                value = render;
            }
            else
            {
                var propertyValue = state.GetPropertyValue(spec.Property);
                
                if (propertyValue is { IsNone: true, IsSourcedFromAttribute: false } && spec.Property.IsOptional)
                    continue;
                
                var jsonValueResult = spec.Renderer.GetJsonNode(context, propertyValue, cancellationToken);
                
                builder.AddDiagnostics(jsonValueResult.Diagnostics);
                
                if(!jsonValueResult.HasValue) continue;

                value = jsonValueResult.Value;
            }
            
            obj[spec.JsonName] = value;
        }

        return builder.Build();
    }

    private static Result<JsonNode> RenderComponentArray(
        IRenderContext<JsonNode> context,
        ComponentPropertyValue propertyValue,
        CancellationToken cancellationToken
    ) => propertyValue
        .AsFlattened
        .Select(x => RenderSingleComponent(context, x, cancellationToken))
        .FlattenAll()
        .Map(JsonNode (components) =>
        {
            var arr = new JsonArray();
            arr.AddRange(components);
            return arr;
        });

    private static Result<JsonNode> RenderSingleComponent(
        IRenderContext<JsonNode> context,
        ComponentPropertyValue propertyValue,
        CancellationToken cancellationToken
    )
    {
        if (propertyValue.AsSingle is not ComponentPropertyValue.Component component)
            return Diagnostic
                .InvalidPropertyValue(propertyValue, ComponentPropertyValueKind.Component)
                .At(propertyValue);

        return component
            .GraphNode
            .Render(context, cancellationToken);
    }

    private static PropertyRenderer Transformed<T>(
        ComponentPropertyValueTransformer<T> transformer,
        Func<T, JsonNode> factory
    ) => (context, propertyValue, cancellationToken) =>
        transformer(context, propertyValue, cancellationToken).Map(factory);

    private static PropertyRenderer Transformed<T>(
        ComponentPropertyValueTransformer<T> transformer,
        Func<T, JsonNodeOptions?, JsonNode> factory,
        JsonNodeOptions? options = null
    ) => (context, propertyValue, cancellationToken) =>
        transformer(context, propertyValue, cancellationToken).Map(v => factory(v, options));

    private static Result<JsonNode> RenderBool(
        IRenderContext<JsonNode> context,
        ComponentPropertyValue propertyValue,
        CancellationToken cancellationToken
    )
    {
        if (propertyValue is { IsNone: true, IsSourcedFromAttribute: true })
            return JsonValue.Create(true);

        if (propertyValue.AsSingle is not ComponentPropertyValue.Literal literal)
            return Diagnostic.TypeMismatch("bool", propertyValue.Kind.ReadableName).At(propertyValue);

        bool? bl = literal.Value.ToLowerInvariant() switch
        {
            "true" => true,
            "false" => false,
            _ => null
        };

        if (bl is null)
            return Diagnostic.TypeMismatch("bool", "string").At(propertyValue);

        return JsonValue.Create(bl.Value);
    }

    private static Result<JsonNode> RenderString(
        IRenderContext<JsonNode> context,
        ComponentPropertyValue propertyValue,
        CancellationToken cancellationToken
    )
    {
        if (propertyValue.AsSingle is not ComponentPropertyValue.Literal literal)
            return Diagnostic.TypeMismatch("string", propertyValue.Kind.ReadableName).At(propertyValue);

        return JsonValue.Create(literal.Value);
    }

    private static Result<JsonNode> RenderNumber(
        IRenderContext<JsonNode> context,
        ComponentPropertyValue propertyValue,
        CancellationToken cancellationToken
    )
    {
        if (propertyValue.AsSingle is not ComponentPropertyValue.Literal literal)
            return Diagnostic.TypeMismatch("number", propertyValue.Kind.ReadableName).At(propertyValue);

        if (!long.TryParse(literal.Value, out var num))
            return Diagnostic.TypeMismatch("number", "string").At(propertyValue);

        return JsonValue.Create(num);
    }
}