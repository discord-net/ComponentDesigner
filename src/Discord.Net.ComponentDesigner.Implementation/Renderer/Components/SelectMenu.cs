using ComponentDesigner;
using ComponentDesigner.CSharp;
using ComponentDesigner.Nodes;
using ComponentDesigner.Parser;
using ComponentDesigner.Util;

namespace Discord;

partial class DiscordNetRenderer
{
    private static readonly CSharpValueTransformer SelectMenuOptions
        = CollectionOf(Symbols.SelectMenuOptionBuilder);

    private static readonly CSharpValueTransformer SelectMenuDefaultValues
        = CollectionOf(Symbols.SelectMenuDefaultValue);

    public static Result<CSharpRender> ChannelType(
        IRenderContext context,
        ComponentPropertyValue value,
        CancellationToken cancellationToken = default
    ) => context
        .CompilationProvider
        .ChannelType(value.TextSpan, cancellationToken)
        .Map(symbol => EnumGenerator.Get(symbol, renderAsSymbolReference: true, allowNullable: false))
        .Map(generator => generator.Render(context, value, cancellationToken));
    
    public static Result<CSharpRender> RenderSelectMenu(
        IRenderContext<CSharpRender> context,
        SelectMenuComponentNode selectMenu,
        SelectMenuState state,
        CancellationToken cancellationToken
    ) => GetSelectMenuType(state.Kind.SourcedAt(state))
        .Map(kind =>
            Construct(
                context,
                state,
                context.CompilationProvider.SelectMenuBuilder,
                cancellationToken,
                ("type", kind),
                ("id", selectMenu.Id, CSharpValueGenerator.NullableInt32),
                ("customId", selectMenu.CustomId, CSharpValueGenerator.String),
                ("placeholder", selectMenu.Placeholder, CSharpValueGenerator.NullableString),
                ("minValues", selectMenu.MinValues, CSharpValueGenerator.NullableInt32),
                ("maxValues", selectMenu.MaxValues, CSharpValueGenerator.NullableInt32),
                ("isRequired", selectMenu.Required, CSharpValueGenerator.NullableBoolean),
                ("isDisabled", selectMenu.Disabled, CSharpValueGenerator.NullableBoolean),
                ("options", selectMenu.Options, SelectMenuOptions),
                ("channelTypes", selectMenu.ChannelTypes, RenderChannelTypes),
                ("defaultValues", selectMenu.DefaultValues, SelectMenuDefaultValues)
            )
        );

    private static Result<CSharpRender> RenderChannelTypes(
        IRenderContext<CSharpRender> context,
        ComponentPropertyValue value,
        CancellationToken cancellationToken
    )
    {
        // handle case of single interpolation (allow for interpolated arrays and such)
        if (value.AsSingle is ComponentPropertyValue.Interpolation interpolation)
            return LoneInterpolated(interpolation);

        return CollectionOf(
            Symbols.ChannelType,
            elementRenderer: (context, value, cancellationToken, out render) =>
            {
                if (value is ComponentPropertyValue.Literal)
                {
                    render = ChannelType(context, value, cancellationToken);
                    return true;
                }

                render = default;
                return false;
            })(context, value, cancellationToken);
        
        Result<CSharpRender> LoneInterpolated(ComponentPropertyValue.Interpolation interpolation)
        {
            if (interpolation.Info.Symbol.Equals(context.CompilationProvider.ChannelType, cancellationToken))
            {
                // simple interpolated value
                return new CSharpRender(
                    interpolation.TextSpan,
                    context.GetReferenceToDesignerValue(
                        interpolation.Info,
                        interpolation.Info.Symbol
                    ),
                    interpolation.Info.Symbol
                );
            }

            if (
                interpolation.Info.Symbol.TryGetEnumerableType(out var enumerableType) &&
                enumerableType.Equals(context.CompilationProvider.ChannelType, cancellationToken)
            )
            {
                return context.CompilationProvider
                    .ListOf(context.CompilationProvider.ChannelType)(interpolation.TextSpan, cancellationToken)
                    .Map(targetListSymbol =>
                    {
                        var designerReference = context.GetReferenceToDesignerValue(
                            interpolation.Info, 
                            interpolation.Info.Symbol
                            );

                        var source = context.CompilationProvider
                            .HasImplicitConversionBetween(
                                interpolation.Info.Symbol,
                                targetListSymbol
                            )
                            ? designerReference
                            : $"new {targetListSymbol.ToQualifiedName()}({designerReference})";
                        
                        return new CSharpRender(
                            interpolation.TextSpan,
                            source,
                            targetListSymbol
                        );
                    });
            }

            return Diagnostic
                .TypeMismatch("ChannelType", interpolation.Info.Symbol)
                .At(interpolation);
        }
    }

    public static Result<CSharpRender> RenderSelectMenuOption(
        IRenderContext<CSharpRender> context,
        SelectMenuOptionComponentNode selectMenuOption,
        ComponentState state,
        CancellationToken cancellationToken
    ) => Construct(
        context,
        state,
        context.CompilationProvider.SelectMenuOptionBuilder,
        cancellationToken,
        ("label", selectMenuOption.Label, CSharpValueGenerator.String),
        ("value", selectMenuOption.Value, CSharpValueGenerator.String),
        ("description", selectMenuOption.Description, CSharpValueGenerator.NullableString),
        ("emoji", selectMenuOption.Emoji, CSharpValueGenerator.Emoji),
        ("isDefault", selectMenuOption.Default, CSharpValueGenerator.NullableBoolean)
    );

    public static Result<CSharpRender> RenderSelectMenuDefaultValue(
        IRenderContext<CSharpRender> context,
        SelectMenuDefaultValueComponentNode selectMenuDefaultValue,
        DefaultValueState state,
        CancellationToken cancellationToken
    ) => Construct(
        context,
        state,
        context.CompilationProvider.SelectMenuDefaultValue,
        cancellationToken,
        ("id", selectMenuDefaultValue.Id, CSharpValueGenerator.UInt64),
        ("type", state.Kind switch
        {
            DefaultValueKind.Channel => "global::Discord.SelectDefaultValueType.Channel",
            DefaultValueKind.User => "global::Discord.SelectDefaultValueType.User",
            DefaultValueKind.Role => "global::Discord.SelectDefaultValueType.Role",
            _ => throw new ArgumentOutOfRangeException(nameof(state.Kind))
        })
    );

    private static Result<string> GetSelectMenuType(
        SourcedValue<SelectMenuKind> kind
    ) => kind.Value switch
    {
        SelectMenuKind.Channel => "global::Discord.ComponentType.ChannelSelect",
        SelectMenuKind.Mentionable => "global::Discord.ComponentType.MentionableSelect",
        SelectMenuKind.Role => "global::Discord.ComponentType.RoleSelect",
        SelectMenuKind.String => "global::Discord.ComponentType.SelectMenu",
        SelectMenuKind.User => "global::Discord.ComponentType.UserSelect",
        _ => Diagnostic
            .TypelessSelectMenu
            .At(kind)
    };
}