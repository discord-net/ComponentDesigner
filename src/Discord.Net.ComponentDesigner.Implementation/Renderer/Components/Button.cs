using ComponentDesigner;
using ComponentDesigner.CSharp;
using ComponentDesigner.Nodes;

namespace Discord;

partial class DiscordNetRenderer
{
    public static Result<CSharpRender> RenderButton(
        IRenderContext<CSharpRender> context,
        ButtonComponentNode button,
        ButtonState state,
        CancellationToken cancellationToken
    ) => Construct(
        context,
        state,
        context.CompilationProvider.ButtonBuilder,
        cancellationToken,
        ("id", button.Id, CSharpValueGenerator.NullableInt32),
        ("style", button.Style, CSharpValueGenerator.ButtonStyle),
        ("label", button.Label, CSharpValueGenerator.NullableString),
        ("emoji", button.Emoji, CSharpValueGenerator.NullableEmoji),
        ("customId", button.CustomId, CSharpValueGenerator.String),
        ("skuId", button.SkuId, CSharpValueGenerator.NullableUInt64),
        ("url", button.Url, CSharpValueGenerator.NullableString),
        ("isDisabled", button.Disabled, CSharpValueGenerator.NullableBoolean)
    );
}