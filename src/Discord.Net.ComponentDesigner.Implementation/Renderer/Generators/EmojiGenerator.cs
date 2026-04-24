using System.Text.RegularExpressions;
using ComponentDesigner;
using ComponentDesigner.CSharp;
using ComponentDesigner.Nodes;
using ComponentDesigner.Parser;
using ComponentDesigner.Util;

namespace Discord;

public sealed class EmojiGenerator : CSharpValueGenerator
{
    private readonly bool _allowNullable;

    private EmojiGenerator(bool allowNullable)
    {
        _allowNullable = allowNullable;
    }

    public static EmojiGenerator Get(bool allowNullable)
        => Memoize.Of(allowNullable, static a => new EmojiGenerator(a));

    protected override Result<CSharpRender> RenderLiteral(
        IRenderContext context,
        ComponentPropertyValue.Literal literalValue,
        string literal,
        CancellationToken cancellationToken = default
    ) => FromText(context, literalValue, cancellationToken);

    protected override Result<CSharpRender> RenderInterpolation(
        IRenderContext context,
        ComponentPropertyValue.Interpolation interpolationValue,
        IInterpolationInfo interpolationInfo,
        CancellationToken cancellationToken = default
    ) => context
        .CompilationProvider
        .IEmote(interpolationInfo, cancellationToken)
        .Map(symbol =>
        {
            if (interpolationInfo.ConstantValue.IsSpecified)
            {
                if (interpolationInfo.ConstantValue.Value is { } value)
                    return FromText(context, symbol, value.ToString().SourcedAt(interpolationValue));

                if (_allowNullable)
                    return new CSharpRender(
                        interpolationInfo.TextSpan,
                        "null",
                        symbol
                    );

                return Diagnostic
                    .NullValueNotAllowed
                    .At(interpolationValue);
            }

            var emoteSymbol = context.CompilationProvider
                .IEmote(interpolationValue, cancellationToken)
                .GetValueOrDefault();

            if (
                context.CompilationProvider.HasImplicitConversionBetween(
                    interpolationInfo.Symbol,
                    emoteSymbol
                )
            )
            {
                return new CSharpRender(
                    interpolationInfo.TextSpan,
                    context.GetReferenceToDesignerValue(interpolationInfo, interpolationInfo.Symbol),
                    interpolationInfo.Symbol
                );
            }

            return Diagnostic
                .TypeMismatch(
                    emoteSymbol,
                    interpolationInfo.Symbol
                )
                .At(interpolationValue);
        });

    private Result<CSharpRender> FromText(
        IRenderContext context,
        SourcedValue<string> text,
        CancellationToken cancellationToken
    ) => context
        .CompilationProvider
        .IEmote(text, cancellationToken)
        .Map(symbol =>
            FromText(context, symbol, text)
        );

    private Result<CSharpRender> FromText(
        IRenderContext context,
        ICSharpTypeSymbol symbol,
        SourcedValue<string> text
    )
    {
        if (text.Value is "null")
        {
            if (_allowNullable)
                return new CSharpRender(
                    text.TextSpan,
                    "null",
                    symbol
                );

            return Diagnostic
                .NullValueNotAllowed
                .At(text);
        }

        return StringGenerator
            .ToCSharpString(context, text.Value)
            .Map(ToSource)
            .Map(source => new CSharpRender(
                text.TextSpan,
                source,
                symbol
            ));

        Result<string> ToSource(string stringForm)
        {
            if (UnicodeEmojiRegex.IsMatch(text))
                return $"global::Discord.Emoji.Parse({stringForm})";

            if (DiscordEmoteRegex.IsMatch(text))
                return $"global::Discord.Emote.Parse({stringForm})";

            var varName = $"_{Guid.NewGuid():N}";

            return (
                $"""
                 global::Discord.Emoji.TryParse({stringForm}, out var {varName})
                    ? (global::Discord.IEmote){varName}
                    : global::Discord.Emote.Parse({stringForm})
                 """,
                Diagnostic
                    .ValueCouldNotBeValidateAndARuntimeValidationCheckWillOccur(
                        "Emoji",
                        text,
                        "Emoji.Parse/Emote.Parse"
                    )
                    .At(text)
            );
        }
    }


    private static readonly Regex UnicodeEmojiRegex = new(
        @"^(?>(?>[\uD800-\uDBFF][\uDC00-\uDFFF]\p{M}*){1,5}|\p{So})$",
        RegexOptions.Compiled
    );

    private static readonly Regex DiscordEmoteRegex = new Regex(@"^<(?>a|):.+:\d+>$", RegexOptions.Compiled);
}