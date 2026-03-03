using ComponentDesigner;
using ComponentDesigner.Nodes;
using ComponentDesigner.Parser;
using ComponentDesigner.Util;

namespace Discord;

partial class DiscordNetRenderer
{
    public override Result<RenderedComponent> RenderSelectMenu(
        IRendererContext context,
        SelectMenuComponentNode selectMenu,
        SelectMenuState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    ) => context.CompilationProvider
        .SelectMenuBuilder(state.TextSpan, cancellationToken)
        .Combine(
            RenderPropertiesAsParameters(
                context, state, cancellationToken,
                explicitParameters: [("type", ToDiscordNetComponentTypeEnum(state.Kind))],
                ("id", selectMenu.Id, CSharpValueGenerator.NullableInteger),
                ("customId", selectMenu.CustomId, CSharpValueGenerator.String),
                ("placeholder", selectMenu.Placeholder, CSharpValueGenerator.NullableString),
                ("minValues", selectMenu.MinValues, CSharpValueGenerator.NullableInteger),
                ("maxValues", selectMenu.MaxValues, CSharpValueGenerator.NullableInteger),
                ("isRequired", selectMenu.Required, CSharpValueGenerator.Boolean),
                ("isDisabled", selectMenu.Disabled, CSharpValueGenerator.Boolean),
                ("options", selectMenu.Options, new(RenderSelectMenuOptionProperty)),
                ("defaultValues", selectMenu.DefaultValues, new(RenderSelectMenuDefaultValuesProperty))
            ),
            (symbol, parameters) => new RenderedComponent(
                $"new {symbol.ToQualifiedName()}({parameters})",
                symbol
            )
        );

    private static Result<string> RenderSelectMenuDefaultValuesProperty(
        IRendererContext context,
        ComponentPropertyValue propertyValue,
        CancellationToken cancellationToken
    )
    {
        return context
            .CompilationProvider
            .SelectMenuDefaultValue(propertyValue, cancellationToken)
            .Map(Render);

        Result<string> Render(ICSharpTypeSymbol symbol)
            => (propertyValue switch
            {
                ComponentPropertyValue.Many many =>
                    many
                        .Values
                        .Select(x => RenderSingle(symbol, x))
                        .FlattenAll()
                        .Map(x => string.Join($",{Environment.NewLine}", x)),
                _ => RenderSingle(symbol, propertyValue)
            }).Map(x =>
                $"""

                 [
                     {x.WithNewlinePadding(4)}
                 ]
                 """
            );

        Result<string> RenderSingle(
            ICSharpTypeSymbol symbol,
            ComponentPropertyValue value
        )
        {
            switch (value)
            {
                case {GraphNode: {} graphNode}:
                    return context
                        .RenderGraphNode(
                            graphNode,
                            new(new(symbol)),
                            cancellationToken
                        )
                        .Map(static x => x.Source);

                case { CXValue: CXValue.Interpolation interpolation }:
                    var info = context.GetInterpolationInfo(interpolation);

                    if (symbol.Equals(info.Symbol!))
                        return context.GetReferenceToDesignerValue(info, info.Symbol);

                    if (info.Symbol.TryGetEnumerableType(out var inner) && inner.Equals(symbol))
                        return $"..{context.GetReferenceToDesignerValue(info, info.Symbol)}";

                    return Diagnostic.TypeMismatch(symbol, info.Symbol!).At(value);

                default:
                    return Diagnostic
                        .InvalidPropertyValue(
                            value,
                            ComponentPropertyValueKind.SyntaxValue,
                            ComponentPropertyValueKind.Component
                        )
                        .At(value);
            }
        }
    }

    private static Result<string> RenderSelectMenuOptionProperty(
        IRendererContext context,
        ComponentPropertyValue propertyValue,
        CancellationToken cancellationToken
    )
    {
        return context
            .CompilationProvider
            .SelectMenuOptionBuilder(propertyValue, cancellationToken)
            .Map(Render);

        Result<string> Render(ICSharpTypeSymbol symbol)
            => (propertyValue switch
            {
                ComponentPropertyValue.Many many =>
                    many
                        .Values
                        .Select(x => RenderSingle(symbol, x))
                        .FlattenAll()
                        .Map(x => string.Join($",{Environment.NewLine}", x)),
                _ => RenderSingle(symbol, propertyValue)
            }).Map(x =>
                $"""

                 [
                     {x.WithNewlinePadding(4)}
                 ]
                 """
            );

        Result<string> RenderSingle(
            ICSharpTypeSymbol symbol,
            ComponentPropertyValue value
        )
        {
            switch (value)
            {
                case {GraphNode: {} graphNode}:
                    return context
                        .RenderGraphNode(
                            graphNode,
                            new(new(symbol)),
                            cancellationToken
                        )
                        .Map(static x => x.Source);

                case { CXValue: CXValue.Interpolation interpolation }:
                    var info = context.GetInterpolationInfo(interpolation);

                    if (symbol.Equals(info.Symbol!))
                        return context.GetReferenceToDesignerValue(info, info.Symbol);

                    if (info.Symbol.TryGetEnumerableType(out var inner) && inner.Equals(symbol))
                        return $"..{context.GetReferenceToDesignerValue(info, info.Symbol)}";

                    return Diagnostic.TypeMismatch(symbol, info.Symbol!).At(value);

                default:
                    return Diagnostic
                        .InvalidPropertyValue(
                            value,
                            ComponentPropertyValueKind.SyntaxValue,
                            ComponentPropertyValueKind.Component
                        )
                        .At(value);
            }
        }
    }

    private static string ToDiscordNetComponentTypeEnum(SelectMenuKind kind)
        => $"global::Discord.ComponentType.{kind switch {
            SelectMenuKind.Channel => "ChannelSelect",
            SelectMenuKind.Mentionable => "MentionableSelect",
            SelectMenuKind.Role => "RoleSelect",
            SelectMenuKind.String => "SelectMenu",
            SelectMenuKind.User => "UserSelect",
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        }}";

    public override Result<RenderedComponent> RenderSelectMenuOption(
        IRendererContext context,
        SelectMenuOptionComponentNode option,
        ComponentState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    ) => context.CompilationProvider
        .SelectMenuOptionBuilder(state.TextSpan, cancellationToken)
        .Combine(
            RenderPropertiesAsParameters(
                context, state, cancellationToken,
                ("label", option.Label, CSharpValueGenerator.String),
                ("value", option.Value, CSharpValueGenerator.String),
                ("description", option.Description, CSharpValueGenerator.NullableString),
                ("emoji", option.Emoji, CSharpValueGenerator.NullableEmoji),
                ("isDefault", option.IsDefault, CSharpValueGenerator.NullableBoolean)
            ),
            (symbol, parameters) => new RenderedComponent(
                $"new {symbol.ToQualifiedName()}({parameters})",
                symbol
            )
        );

    public override Result<RenderedComponent> RenderSelectMenuDefaultValue(
        IRendererContext context,
        SelectMenuDefaultValueComponentNode option,
        DefaultValueState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    ) => context.CompilationProvider
        .SelectMenuDefaultValue(state.TextSpan, cancellationToken)
        .Combine(
            RenderSelectMenuDefaultValueIdParameter(context, option, state, cancellationToken),
            (symbol, id) => new RenderedComponent(
                $"""
                 new {symbol.ToQualifiedName()}(
                     id: {id},
                     type: global::Discord.SelectDefaultValueType.{state.Kind switch {
                         DefaultValueKind.Channel => "Channel",
                         DefaultValueKind.User => "User",
                         DefaultValueKind.Role => "Role",
                         _ => throw new ArgumentOutOfRangeException(nameof(state.Kind))
                     }}
                 )
                 """
            )
        );

    private static Result<string> RenderSelectMenuDefaultValueIdParameter(
        IRendererContext context,
        SelectMenuDefaultValueComponentNode option,
        DefaultValueState state,
        CancellationToken cancellationToken = default
    )
    {
        // TODO: allow interpolation of entities
        return CSharpValueGenerator.Snowflake.Render(
            context,
            state.GetPropertyValue(option.Id),
            cancellationToken: cancellationToken
        );
    }
}