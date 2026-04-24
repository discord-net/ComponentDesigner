using System.Text.Json.Nodes;
using System.Threading;
using ComponentDesigner.Nodes;

namespace ComponentDesigner.Json;

partial class JsonRenderer
{
    public const int STRING_SELECT_TYPE = 3;
    public const int USER_SELECT_TYPE = 5;
    public const int ROLE_SELECT_TYPE = 6;
    public const int MENTIONABLE_SELECT_TYPE = 7;
    public const int CHANNEL_SELECT_TYPE = 8;

    public Result<JsonNode> RenderSelectMenu(
        IRenderContext<JsonNode> context,
        SelectMenuComponentNode selectMenu,
        SelectMenuState state,
        CancellationToken cancellationToken = default
    )
    {
        int? type = state.Kind switch
        {
            SelectMenuKind.Channel => CHANNEL_SELECT_TYPE,
            SelectMenuKind.Mentionable => MENTIONABLE_SELECT_TYPE,
            SelectMenuKind.Role => ROLE_SELECT_TYPE,
            SelectMenuKind.String => STRING_SELECT_TYPE,
            SelectMenuKind.User => USER_SELECT_TYPE,
            _ => null
        };

        if (type is null) return Diagnostic.TypelessSelectMenu.At(state.ElementIdentifierTextSpanOrBetter);

        return Spec(
            context,
            state,
            cancellationToken,
            ("type", type.Value),
            ("id", selectMenu.Id, Number),
            ("custom_id", selectMenu.CustomId, String),
            ("options", selectMenu.Options, ComponentArray),
            ("channel_types", selectMenu.ChannelTypes, ComponentArray),
            ("placeholder", selectMenu.Placeholder, String),
            ("default_values", selectMenu.DefaultValues, ComponentArray),
            ("min_values", selectMenu.MinValues, Number),
            ("max_values", selectMenu.MaxValues, Number),
            ("required", selectMenu.Required, Bool),
            ("disabled", selectMenu.Disabled, Bool)
        );
    }

    public Result<JsonNode> RenderSelectMenuOption(
        IRenderContext<JsonNode> context,
        SelectMenuOptionComponentNode option,
        ComponentState state,
        CancellationToken cancellationToken = default
    ) => Spec(
        context,
        state,
        cancellationToken,
        ("label", option.Label, String),
        ("value", option.Value, String),
        ("description", option.Description, String),
        ("emoji", option.Emoji, Emoji),
        ("default", option.Default, Bool)
    );

    public Result<JsonNode> RenderSelectMenuDefaultValue(
        IRenderContext<JsonNode> context,
        SelectMenuDefaultValueComponentNode option,
        DefaultValueState state,
        CancellationToken cancellationToken = default
    )
    {
        var type = state.Kind switch
        {
            DefaultValueKind.Channel => "channel",
            DefaultValueKind.Role => "role",
            DefaultValueKind.User => "user",
            _ => null
        };
        
        if(type is null) return Result<JsonNode>.Empty;

        return Spec(
            context,
            state,
            cancellationToken,
            ("type", type),
            ("id", option.Id, String)
        );
    }
}