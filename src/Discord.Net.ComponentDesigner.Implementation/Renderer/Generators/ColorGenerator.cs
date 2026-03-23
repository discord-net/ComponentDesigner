using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using ComponentDesigner;
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

    protected override Result<string> RenderLiteral(
        IRendererContext context,
        ComponentPropertyValue.Literal literalValue,
        string literal,
        CancellationToken cancellationToken = default
    ) => FromText(context, literalValue, cancellationToken);

    protected override Result<string> RenderInterpolation(
        IRendererContext context,
        ComponentPropertyValue.Interpolation interpolationValue,
        IInterpolationInfo interpolationInfo,
        CancellationToken cancellationToken = default
    )
    {
        if (interpolationInfo.ConstantValue.IsSpecified)
        {
            if (interpolationInfo.ConstantValue.Value is { } value)
                return FromText(context, value.ToString().SourcedAt(interpolationValue), cancellationToken);

            if (_allowNullable) return "null";

            return Diagnostic
                .NullValueNotAllowed
                .At(interpolationValue);
        }

        var colorSymbol = context.CompilationProvider.GetTypeFromQualifiedName(DiscordColorTypeName, cancellationToken);

        if (
            colorSymbol is not null &&
            (
                context.CompilationProvider.HasImplicitConversionBetween(
                    interpolationInfo.Symbol,
                    colorSymbol
                )
                ||
                (
                    _allowNullable &&
                    interpolationInfo.Symbol.IsNullableTypeOf(colorSymbol)
                )
            )
        )
        {
            return context.GetReferenceToDesignerValue(interpolationInfo, interpolationInfo.Symbol);
        }

        return Diagnostic
            .TypeMismatch(
                DiscordColorTypeName,
                interpolationInfo.Symbol!
            )
            .At(interpolationValue);
    }

    private Result<string> FromText(
        IRendererContext context,
        SourcedValue<string> text,
        CancellationToken cancellationToken
    )
    {
        if (TryGetColorPreset(context, text, out var preset, cancellationToken)) return preset;

        if (TryGetHexColor(text, out var hex)) return hex;

        return UseLibraryParseFunc(context, text);
    }

    private Result<string> UseLibraryParseFunc(
        IRendererContext context,
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
        IRendererContext context,
        string presetName,
        [MaybeNullWhen(false)] out string preset,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(presetName))
        {
            preset = null;
            return false;
        }

        var symbol = context.CompilationProvider.GetTypeFromQualifiedName(DiscordColorTypeName, cancellationToken);

        preset = symbol
            ?.Fields
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