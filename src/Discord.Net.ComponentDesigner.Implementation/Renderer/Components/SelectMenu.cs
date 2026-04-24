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
                ("defaultValues", selectMenu.DefaultValues, SelectMenuDefaultValues)
            )
        );

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