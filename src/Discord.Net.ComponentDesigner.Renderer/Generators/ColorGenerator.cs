using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using ComponentDesigner;
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
            if (info.ConstantValue.Value is string str) return FromText(context, token.TextSpan, str);

            if (
                info.ConstantValue.Value?.GetType().IsNumeric is true &&
                uint.TryParse(info.ConstantValue.Value?.ToString(), out var hex)
            )
            {
                return $"new global::{DiscordColorTypeName}({hex})";
            }
        }

        var colorSymbol = context.CompilationProvider.GetTypeFromQualifiedName(DiscordColorTypeName);

        if (
            colorSymbol is not null && (
                context.CompilationProvider.HasImplicitConversionBetween(
                    info.Symbol,
                    colorSymbol
                )
                ||
                (
                    _allowNullable &&
                    info.Symbol.IsNullableTypeOf(colorSymbol)
                )
            )
        )
        {
            return context.GetReferenceToDesignerValue(info, info.Symbol);
        }

        return UseLibraryParseFunction(
            context,
            token.TextSpan,
            context.GetReferenceToDesignerValue(info),
            valueIsNullable: info.Symbol.CanNullPatternMatch
        );
    }

    protected override Result<string> RenderScalar(
        IRendererContext context,
        CSharpValueGeneratorTarget target,
        CXToken token,
        CSharpValueGeneratorOptions options,
        CancellationToken cancellationToken = default
    ) => FromText(context, token.TextSpan, token.Value);

    protected override Result<string> RenderMultipart(
        IRendererContext context,
        CSharpValueGeneratorTarget target,
        CXValue.Multipart multipart,
        CSharpValueGeneratorOptions options,
        CancellationToken cancellationToken = default
    ) => UseLibraryParseFunction(
        context,
        multipart.TextSpan,
        StringGenerator.ToCSharpString(multipart),
        valueIsNullable: false
    );

    private Result<string> FromText(IRendererContext context, CXTextSpan textSpan, string text)
    {
        if (TryGetColorPreset(context, text, out var preset)) 
            return $"global::Discord.Color.{preset}";

        var hex = text;

        if (hex.StartsWith("#")) hex = hex.Substring(1);

        if (
            uint.TryParse(
                hex,
                NumberStyles.HexNumber,
                null,
                out var hexCode
            )
        )
        {
            return $"new global::{DiscordColorTypeName}({hexCode})";
        }

        return UseLibraryParseFunction(context, textSpan, text, valueIsNullable: false);
    }

    private Result<string> UseLibraryParseFunction(
        IRendererContext context,
        CXTextSpan textSpan,
        string value,
        bool valueIsNullable
    )
    {
        if (!_allowNullable && valueIsNullable)
            return textSpan.Report(Diagnostic.NullValueNotAllowed);

        if (string.IsNullOrWhiteSpace(value))
            return textSpan.Report(Diagnostic.EmptyValueNotAllowed);

        string code;

        if (valueIsNullable)
        {
            var varName = context.CreateVariable();
            code =
                $$"""
                  {{value}} is {} {{varName}}
                      ? global::{{DiscordColorTypeName}}.Parse({{varName}}.ToString())
                      : null
                  """;
        }
        else
        {
            code = $"global::{DiscordColorTypeName}.Parse({value})";
        }

        return (
            code,
            textSpan.Report(Diagnostic.UsingRuntimeValidation($"{DiscordColorTypeName}.Parse"))
        );
    }

    private static bool TryGetColorPreset(
        IRendererContext context,
        string name,
        [MaybeNullWhen(false)] out string preset
    )
    {
        var symbol = context.CompilationProvider.GetTypeFromQualifiedName(DiscordColorTypeName);

        if (symbol is null)
        {
            preset = null;
            return false;
        }

        var field = symbol
            .Fields
            .FirstOrDefault(x =>
                x is { IsPublic: true, IsStatic: true, IsReadOnly: true } &&
                x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase) &&
                x.Type.Equals(symbol)
            );

        if (field is not null)
        {
            preset = field.ToQualifiedName();
            return true;
        }

        preset = null;
        return false;
    }
}