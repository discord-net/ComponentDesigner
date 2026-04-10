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
                ("id", selectMenu.Id, CSharpValueGenerator.NullableInt32),
                ("customId", selectMenu.CustomId, CSharpValueGenerator.String),
                ("placeholder", selectMenu.Placeholder, CSharpValueGenerator.NullableString),
                ("minValues", selectMenu.MinValues, CSharpValueGenerator.NullableInt32),
                ("maxValues", selectMenu.MaxValues, CSharpValueGenerator.NullableInt32),
                ("isRequired", selectMenu.Required, CSharpValueGenerator.Boolean),
                ("isDisabled", selectMenu.Disabled, CSharpValueGenerator.Boolean),
                ("options", selectMenu.Options, new(RenderSelectMenuOptionProperty)),
                ("defaultValues", selectMenu.DefaultValues, new(RenderSelectMenuDefaultValuesProperty))
            ),
            (symbol, parameters) => new RenderedComponent(
                $"new {symbol.ToQualifiedName()}({parameters})",
                symbol
            )
        )
        .Map(ApplyRefParameter(context, state, cancellationToken))
        .Map(GetConverterFromOptions(context, state, typingContext, cancellationToken));

    private static Result<string> RenderSelectMenuDefaultValuesProperty(
        IRendererContext context,
        ComponentPropertyValue propertyValue,
        CancellationToken cancellationToken
    )
    {
        return RenderGenericArrayOfValue(
            context,
            propertyValue,
            cancellationToken,
            componentHandler: RenderComponentValue,
            interpolationHandler: RenderInterpolation
        );

        static Result<string> RenderComponentValue(
            IRendererContext context,
            ComponentPropertyValue.Component component,
            CancellationToken cancellationToken
        ) => context
            .RenderGraphNode(
                component.GraphNode,
                cancellationToken: cancellationToken
            )
            .AsSource;

        static Result<string> RenderInterpolation(
            IRendererContext context,
            ComponentPropertyValue.Interpolation interpolation,
            CancellationToken cancellationToken
        )
        {
            if (interpolation.Info.Symbol is null)
                return Diagnostic
                    .TypeMismatch(
                        "unknown",
                        "SelectMenuDefaultValue"
                    )
                    .At(interpolation);

            if (interpolation.Info.Symbol.Equals(context.CompilationProvider.SelectMenuDefaultValue, cancellationToken))
                return context.GetReferenceToDesignerValue(
                    interpolation.Info,
                    interpolation.Info.Symbol
                );

            if (
                interpolation.Info.Symbol.Equals(
                    context.CompilationProvider.IEnumerableOf(context.CompilationProvider.SelectMenuDefaultValue),
                    cancellationToken
                )
            )
            {
                return $"..{context.GetReferenceToDesignerValue(
                    interpolation.Info,
                    interpolation.Info.Symbol
                )}";
            }

            return Diagnostic
                .TypeMismatch(
                    interpolation.Info.Symbol,
                    "SelectMenuDefaultValue"
                )
                .At(interpolation);
        }
    }

    private static Result<string> RenderSelectMenuOptionProperty(
        IRendererContext context,
        ComponentPropertyValue propertyValue,
        CancellationToken cancellationToken
    )
    {
        return RenderGenericArrayOfValue(
            context,
            propertyValue,
            cancellationToken,
            componentHandler: RenderComponentValue,
            interpolationHandler: RenderInterpolation
        );

        static Result<string> RenderComponentValue(
            IRendererContext context,
            ComponentPropertyValue.Component component,
            CancellationToken cancellationToken
        ) => context
            .CompilationProvider
            .IEnumerableOf(context.CompilationProvider.SelectMenuOptionBuilder, component, cancellationToken)
            .Map(symbol => context
                .RenderGraphNode(
                    component.GraphNode,
                    options: new(new(symbol)),
                    cancellationToken: cancellationToken
                )
                .AsSource
            );

        static Result<string> RenderInterpolation(
            IRendererContext context,
            ComponentPropertyValue.Interpolation interpolation,
            CancellationToken cancellationToken
        )
        {
            if (interpolation.Info.Symbol is null)
                return Diagnostic
                    .TypeMismatch(
                        "unknown",
                        "SelectMenuOption"
                    )
                    .At(interpolation);

            if (interpolation.Info.Symbol.Equals(context.CompilationProvider.SelectMenuOptionBuilder,
                    cancellationToken))
                return context.GetReferenceToDesignerValue(
                    interpolation.Info,
                    interpolation.Info.Symbol
                );

            if (
                interpolation.Info.Symbol.Equals(
                    context.CompilationProvider.IEnumerableOf(context.CompilationProvider.SelectMenuOptionBuilder),
                    cancellationToken
                )
            )
            {
                return $"..{context.GetReferenceToDesignerValue(
                    interpolation.Info,
                    interpolation.Info.Symbol
                )}";
            }

            return Diagnostic
                .TypeMismatch(
                    interpolation.Info.Symbol,
                    "SelectMenuOption"
                )
                .At(interpolation);
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
        )
        .Map(GetConverterFromOptions(context, state, typingContext, cancellationToken));

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
        )
        .Map(GetConverterFromOptions(context, state, typingContext, cancellationToken));

    private static Result<string> RenderSelectMenuDefaultValueIdParameter(
        IRendererContext context,
        SelectMenuDefaultValueComponentNode option,
        DefaultValueState state,
        CancellationToken cancellationToken = default
    )
    {
        // TODO: allow interpolation of entities
        return CSharpValueGenerator.UInt64.Render(
            context,
            state.GetPropertyValue(option.Id),
            cancellationToken: cancellationToken
        );
    }
}