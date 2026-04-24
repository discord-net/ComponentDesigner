using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using ComponentDesigner;
using ComponentDesigner.CSharp;
using ComponentDesigner.Nodes;
using ComponentDesigner.Parser;
using ComponentDesigner.Util;

namespace Discord;

public sealed class ColorGenerator : CSharpValueGenerator
{
    private const string DiscordColorTypeName = "Discord.Color";

    private readonly bool _allowNullable;

    private ColorGenerator(bool allowNullable)
    {
        _allowNullable = allowNullable;
    }

    public static ColorGenerator Get(bool allowNullable)
        => WeakMemoize.Of(allowNullable, static a => new ColorGenerator(a));

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
        .Color(interpolationInfo.TextSpan, cancellationToken)
        .Map(symbol =>
        {
            if (interpolationInfo.ConstantValue.IsSpecified)
            {
                if (interpolationInfo.ConstantValue.Value is { } value)
                    return FromText(context, value.ToString().SourcedAt(interpolationValue), cancellationToken);

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
            
            if (
                context.CompilationProvider.HasImplicitConversionBetween(
                    interpolationInfo.Symbol,
                    symbol
                )
                ||
                (
                    _allowNullable &&
                    interpolationInfo.Symbol.IsNullableTypeOf(symbol)
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
                    DiscordColorTypeName,
                    interpolationInfo.Symbol!
                )
                .At(interpolationValue);
        });

    private Result<CSharpRender> FromText(
        IRenderContext context,
        SourcedValue<string> text,
        CancellationToken cancellationToken
    ) => context
        .CompilationProvider
        .Color(text.TextSpan, cancellationToken)
        .Combine(symbol =>
            {
                if (TryGetColorPreset(symbol, text.Value, out var preset)) return preset;

                if (TryGetHexColor(text, out var hex)) return hex;

                return UseLibraryParseFunc(context, text);
            },
            (symbol, source) => new CSharpRender(
                text.TextSpan,
                source,
                symbol
            )
        );

    private Result<string> UseLibraryParseFunc(
        IRenderContext context,
        SourcedValue<string> text
    )
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Diagnostic
                .EmptyValueNotAllowed
                .At(text);
        }

        if (text.Value is "null")
        {
            if (_allowNullable) return "null";

            return Diagnostic
                .NullValueNotAllowed
                .At(text);
        }

        return (
            $"global::{DiscordColorTypeName}.Parse({StringGenerator.ToCSharpString(context, text)})",
            Diagnostic
                .UsingRuntimeValidation($"{DiscordColorTypeName}.Parse")
                .At(text)
        );
    }

    private static bool TryGetHexColor(
        string text,
        [MaybeNullWhen(false)] out string result
    )
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            result = null;
            return false;
        }

        if (text.StartsWith("#"))
            text = text.Substring(1);

        if (uint.TryParse(text, NumberStyles.HexNumber, null, out var hexCode))
        {
            result = $"new global::{DiscordColorTypeName}({hexCode})";
            return true;
        }

        result = null;

        return false;
    }

    private static bool TryGetColorPreset(
        ICSharpTypeSymbol symbol,
        string presetName,
        [MaybeNullWhen(false)] out string preset
    )
    {
        if (string.IsNullOrWhiteSpace(presetName))
        {
            preset = null;
            return false;
        }

        preset = symbol
            .Fields
            .FirstOrDefault(x =>
                IsColorPresetField(x) &&
                x.Name.Equals(presetName, StringComparison.InvariantCultureIgnoreCase)
            )
            ?.ToQualifiedName();

        return preset is not null;

        static bool IsColorPresetField(ICSharpFieldSymbol symbol)
            => symbol is { IsPublic: true, IsStatic: true, IsReadOnly: true } &&
               symbol.Type.Equals(symbol.ContainingType);
    }
}