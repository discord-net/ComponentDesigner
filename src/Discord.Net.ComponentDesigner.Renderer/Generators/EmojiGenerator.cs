using System.Text.RegularExpressions;
using ComponentDesigner;
using ComponentDesigner.Parser;
using ComponentDesigner.Util;

namespace Discord.ComponentDesigner;

public sealed class EmojiGenerator : CSharpValueGenerator
{
    private readonly bool _allowNullable;

    private EmojiGenerator(bool allowNullable)
    {
        _allowNullable = allowNullable;
    }

    public static EmojiGenerator Get(bool allowNullable)
        => Memoize.Of(allowNullable, static a => new EmojiGenerator(a));

    protected override Result<string> RenderScalar(
        IRendererContext context,
        CSharpValueGeneratorTarget target,
        CXToken token,
        CSharpValueGeneratorOptions options,
        CancellationToken cancellationToken = default
    ) => FromText(context, token.Span, token.Value);

    protected override Result<string> RenderInterpolation(
        IRendererContext context,
        CSharpValueGeneratorTarget target,
        CXToken token,
        IInterpolationInfo info,
        CSharpValueGeneratorOptions options,
        CancellationToken cancellationToken = default
    )
    {
        if (info.ConstantValue.IsSpecified)
        {
            if (info.ConstantValue.Value is null)
            {
                if (_allowNullable) return "null";

                return Diagnostic.NullValueNotAllowed.At(token);
            }

            if (info.ConstantValue.Value is string str) return FromText(context, token.Span, str);
        }

        return context.CompilationProvider
            .IEmote(info.TextSpan, cancellationToken)
            .Map(emoteSymbol =>
            {
                if (
                    context.CompilationProvider.HasImplicitConversionBetween(
                        info.Symbol,
                        emoteSymbol
                    )
                )
                {
                    return Result<string>.FromValue(context.GetReferenceToDesignerValue(info, emoteSymbol));
                }

                return Diagnostic.TypeMismatch(emoteSymbol, info.Symbol!).At(token.Span);
            });
    }

    private static Result<string> FromText(
        IRendererContext context,
        CXTextSpan textSpan,
        string text
    )
    {
        var stringForm = StringGenerator.ToCSharpString(text);

        if (UnicodeEmojiRegex.IsMatch(text))
            return $"global::Discord.Emoji.Parse({stringForm})";

        if (DiscordEmoteRegex.IsMatch(text))
            return $"global::Discord.Emote.Parse({stringForm})";

        var varName = context.CreateVariable("emoji");

        return (
            $"""
             global::Discord.Emoji.TryParse({stringForm}, out var {varName})
                ? (global::Discord.IEmote){varName}
                : global::Discord.Emote.Parse({stringForm})
             """,
            Diagnostic
                .ValueCouldNotBeValidateAndARuntimeValidationCheckWillOccur("Emoji", text, "Emoji.Parse/Emote.Parse")
                .At(textSpan)
        );
    }

    private static readonly Regex UnicodeEmojiRegex = new(
        @"^(?>(?>[\uD800-\uDBFF][\uDC00-\uDFFF]\p{M}*){1,5}|\p{So})$",
        RegexOptions.Compiled
    );

    private static readonly Regex DiscordEmoteRegex = new Regex(@"^<(?>a|):.+:\d+>$", RegexOptions.Compiled);
}