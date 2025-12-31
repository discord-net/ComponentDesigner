using System.Text.RegularExpressions;
using Discord.CX.Parser;
using Microsoft.CodeAnalysis;

namespace Discord.CX.Nodes;

public sealed class EmojiGenerator : CXValueGenerator
{
    protected override Result<string> RenderInterpolation(
        IComponentContext context,
        CXValueGeneratorTarget target,
        CXToken token,
        DesignerInterpolationInfo info,
        CXValueGeneratorOptions options
    )
    {
        if (
            context.Compilation.HasImplicitConversion(
                info.Symbol,
                context.KnownTypes.IEmoteType
            )
        )
        {
            return context.GetDesignerValue(
                info,
                context.KnownTypes.IEmoteType
            );
        }

        if (info.Constant is { HasValue: true, Value: string str })
        {
            var builder = new Result<string>.Builder();

            LightlyValidateEmote(ref builder, str, token, out var isDiscordEmote, out var isUnicodeEmoji);
            UseLibraryParse(ref builder, context, token, StringGenerator.ToCSharpString(str), isUnicodeEmoji, isDiscordEmote);

            return builder.Build();
        }

        return UseLibraryParse(
            context,
            token,
            context.GetDesignerValue(info)
        );
    }

    protected override Result<string> RenderScalar(
        IComponentContext context,
        CXValueGeneratorTarget target,
        CXToken token,
        CXValueGeneratorOptions options
    )
    {
        var builder = new Result<string>.Builder();

        LightlyValidateEmote(ref builder, token.Value, token, out var isDiscordEmote, out var isUnicodeEmoji);
        UseLibraryParse(
            ref builder,
            context,
            token,
            StringGenerator.ToCSharpString(token.Value),
            isUnicodeEmoji,
            isDiscordEmote
        );

        return builder.Build();
    }

    protected override Result<string> RenderMultipart(
        IComponentContext context,
        CXValueGeneratorTarget target,
        CXValue.Multipart multipart,
        CXValueGeneratorOptions options
    ) => UseLibraryParse(
        context,
        multipart,
        StringGenerator.ToCSharpString(multipart)
    );

    private static void LightlyValidateEmote(
        ref Result<string>.Builder builder,
        string emote,
        ICXNode node,
        out bool isDiscordEmote,
        out bool isUnicodeEmoji
    )
    {
        isDiscordEmote = IsDiscordEmote.IsMatch(emote);
        isUnicodeEmoji = !isDiscordEmote && IsEmoji.IsMatch(emote);

        if (!isDiscordEmote && !isUnicodeEmoji)
        {
            builder.AddDiagnostic(
                Diagnostics.PossibleInvalidEmote(emote),
                node
            );
        }
    }

    private static Result<string> UseLibraryParse(
        IComponentContext context,
        ICXNode owner,
        string code,
        bool isUnicodeEmoji = false,
        bool isDiscordEmote = false
    )
    {
        var builder = new Result<string>.Builder();
        UseLibraryParse(ref builder, context, owner, code, isUnicodeEmoji, isDiscordEmote);
        return builder.Build();
    }

    private static void UseLibraryParse(
        ref Result<string>.Builder builder,
        IComponentContext context,
        ICXNode owner,
        string code,
        bool isUnicodeEmoji = false,
        bool isDiscordEmote = false
    )
    {
        string parseFunc;

        if (isUnicodeEmoji)
            parseFunc = $"global::Discord.Emoji.Parse({code})";
        else if (isDiscordEmote)
            parseFunc = $"global::Discord.Emote.Parse({code})";
        else
        {
            var varName = context.GetVariableName("emoji");
            parseFunc =
                $"""
                 global::Discord.Emoji.TryParse({code}, out var {varName})
                    ? (global::Discord.IEmote){varName}
                    : global::Discord.Emote.Parse({context})
                 """;
            
            builder.AddDiagnostic(
                Diagnostics.FallbackToRuntimeValueParsing("Emoji.Parse/Emote.Parse"),
                owner
            );
        }


        builder.WithValue(parseFunc);
    }

    private static readonly Regex IsEmoji = new(
        @"^(?>(?>[\uD800-\uDBFF][\uDC00-\uDFFF]\p{M}*){1,5}|\p{So})$",
        RegexOptions.Compiled
    );

    private static readonly Regex IsDiscordEmote = new Regex(@"^<(?>a|):.+:\d+>$", RegexOptions.Compiled);
}