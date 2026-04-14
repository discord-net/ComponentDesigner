using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using ComponentDesigner.Nodes;
using ComponentDesigner;
using ComponentDesigner.Nodes.TextControls;
using ComponentDesigner.Util;

namespace ComponentDesigner.Json;

public sealed partial class JsonRenderer : IComponentRenderer
{
    public JsonSerializerOptions? JsonSerializerOptions { get; }

    public JsonRenderer(JsonSerializerOptions? jsonSerializerOptions = null)
    {
        JsonSerializerOptions = jsonSerializerOptions;
    }

    private static readonly PropertyRenderer UnfurledMediaItem = static (context, propertyValue, cancellationToken) =>
        PropertyTransformer.String(context, propertyValue, cancellationToken).Map(JsonNode (url) => new JsonObject()
        {
            ["url"] = url
        });

    private static readonly PropertyRenderer Bool = Value(ValueKind.Bool);
    private static readonly PropertyRenderer String = Value(ValueKind.String);
    private static readonly PropertyRenderer Number = Value(ValueKind.Number);
    private static readonly PropertyRenderer Color = Transformed(PropertyTransformer.ColorCode, JsonValue.Create);

    private static readonly PropertyRenderer Emoji = Transformed(PropertyTransformer.PartialEmoji, JsonNode (emoji) =>
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

    private static readonly PropertyRenderer Component = static (context, propertyValue, cancellationToken) =>
    {
        if (propertyValue.AsSingle is not ComponentPropertyValue.Component component)
            return Diagnostic
                .InvalidPropertyValue(propertyValue, ComponentPropertyValueKind.Component)
                .At(propertyValue);

        return context
            .RenderGraphNode(
                component.GraphNode,
                cancellationToken: cancellationToken
            )
            .Map(Result<JsonNode> (x) =>
            {
                if (x is not RenderedJsonComponent jsonComponent)
                    return Diagnostic.TypeMismatch("json", x.GetType().Name).At(propertyValue);

                return jsonComponent.JsonNode;
            });
    };

    private static readonly PropertyRenderer Components = static (context, propertyValue, cancellationToken) =>
        propertyValue
            .AsFlattened
            .Select(value => Component(context, value, cancellationToken))
            .FlattenAll()
            .Map(JsonNode (col) =>
            {
                var arr = new JsonArray();
                arr.AddRange(col);
                return arr;
            });


    [Flags]
    private enum ValueKind
    {
        String = 1 << 0,
        Bool = 1 << 1,
        Number = 1 << 2,
        Array = 1 << 3,
        IncludeNull = 1 << 4
    }

    private delegate Result<JsonNode> PropertyRenderer(
        IRendererContext context,
        ComponentPropertyValue propertyValue,
        CancellationToken cancellationToken
    );

    private static PropertyRenderer Transformed<T>(ComponentPropertyValueTransformer<T> transformer,
        Func<T, JsonNode> factory)
        => (context, propertyValue, cancellationToken) =>
            transformer(context, propertyValue, cancellationToken).Map(factory);

    private static PropertyRenderer Transformed<T>(
        ComponentPropertyValueTransformer<T> transformer,
        Func<T, JsonNodeOptions?, JsonNode> factory,
        JsonNodeOptions? options = null
    ) => (context, propertyValue, cancellationToken) =>
        transformer(context, propertyValue, cancellationToken).Map(v => factory(v, options));


    private static PropertyRenderer Value(ValueKind kind)
    {
        switch (kind)
        {
            case ValueKind.Number:
                return BuildNumber;
            case ValueKind.String:
                return BuildString;
            case ValueKind.Bool:
                return BuildBool;
        }

        throw new NotImplementedException();

        Result<JsonNode> BuildNumber(
            IRendererContext context,
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

        Result<JsonNode> BuildString(
            IRendererContext context,
            ComponentPropertyValue propertyValue,
            CancellationToken cancellationToken
        )
        {
            if (propertyValue.AsSingle is not ComponentPropertyValue.Literal literal)
                return Diagnostic.TypeMismatch("string", propertyValue.Kind.ReadableName).At(propertyValue);

            return JsonValue.Create(literal.Value);
        }

        Result<JsonNode> BuildBool(
            IRendererContext context,
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
    }

    private static PropertyRenderer Enum(
        params (string Name, int Value)[] members
    ) => (context, propertyValue, cancellationToken) =>
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
    };

    private static Result<RenderedComponent> Build(
        IRendererContext context,
        ComponentState state,
        CancellationToken cancellationToken,
        params (string, ComponentProperty, PropertyRenderer)[] properties
    ) => Build(context, state, cancellationToken, [], properties);

    private static Result<RenderedComponent> Build(
        IRendererContext context,
        ComponentState state,
        CancellationToken cancellationToken,
        (string, JsonNode)[] initialProperties,
        params (string, ComponentProperty, PropertyRenderer)[] properties
    )
    {
        var obj = new JsonObject();
        foreach (var (name, value) in initialProperties)
        {
            obj[name] = value;
        }

        return AddProperties(obj, context, state, cancellationToken, properties)
            .Map(AsRendered);
    }

    private static Result<JsonObject> AddProperties(
        JsonObject obj,
        IRendererContext context,
        ComponentState state,
        CancellationToken cancellationToken,
        params (string, ComponentProperty, PropertyRenderer)[] properties
    )
    {
        using var builder = Result<JsonObject>.Builder.WithValue(obj);

        for (var i = 0; i < properties.Length; i++)
        {
            var (name, property, renderer) = properties[i];
            var propertyValue = state.GetPropertyValue(property);

            if (propertyValue is { IsNone: true, IsSourcedFromAttribute: false } && property.IsOptional)
                continue;

            var jsonValueResult = renderer(context, propertyValue, cancellationToken);

            if (jsonValueResult.HasValue)
                obj[name] = jsonValueResult.Value;

            builder.AddDiagnostics(jsonValueResult.Diagnostics);
        }

        return builder.Build();
    }

    private static RenderedComponent AsRendered(JsonObject obj)
        => new RenderedJsonComponent(obj);

    public Result<RenderedComponent> RenderTextControls(
        IRendererContext context,
        TextControlGraph textControlGraph,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    )
    {
        return textControlGraph
            .Render(
                context,
                new(string.Empty, string.Empty),
                cancellationToken
            )
            .Map(RenderedComponent (text) =>
            {
                text = text.Trim().NormalizeIndentation();

                return new RenderedJsonComponent(
                    JsonValue.Create(text)
                );
            });
    }

    public Result<string> RenderComponents(
        CXComponentGraph graph,
        ComponentEmitContext context,
        CancellationToken cancellationToken = default
    ) => graph.RootNodes
        .Select(x => context.RenderGraphNode(x, cancellationToken: cancellationToken))
        .FlattenAll()
        .Map(x =>
        {
            var arr = new JsonArray();
            arr.AddRange(x.OfType<RenderedJsonComponent>().Select(x => x.JsonNode));
            return arr.ToJsonString(JsonSerializerOptions);
        });

    public Result<RenderedComponent> RenderFunctionalComponent(
        IRendererContext context,
        FunctionalComponentNode functionalComponent,
        FunctionalState state, RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    ) => Diagnostic.TypedComponentsAreNotSupported("json").At(state.ElementIdentifierTextSpanOrBetter);

    public Result<RenderedComponent> RenderInterpolation(
        IRendererContext context,
        IInterpolationInfo info,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    ) => Diagnostic.TypedComponentsAreNotSupported("json").At(info);
}