using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Discord.CX.Parser;
using Microsoft.CodeAnalysis;

namespace Discord.CX.Nodes;

public sealed class ColorGenerator : CXValueGenerator
{
    private static readonly Dictionary<string, IReadOnlyDictionary<string, string>> _fieldMaps = [];

    protected override Result<string> RenderInterpolation(
        IComponentContext context,
        CXValueGeneratorTarget target,
        CXToken token,
        DesignerInterpolationInfo info,
        CXValueGeneratorOptions options
    )
    {
        if (info.Constant.HasValue)
        {
            if (info.Constant.Value is string str)
                return FromText(context, token, str);

            if (
                (info.Constant.Value?.GetType().IsNumericType() ?? false) &&
                uint.TryParse(info.Constant.Value.ToString(), out var hexColor)
            )
            {
                return $"new {
                    context.KnownTypes.ColorType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                }({hexColor})";
            }
        }

        if (
            context.Compilation.HasImplicitConversion(
                info.Symbol,
                context.KnownTypes.ColorType
            )
        )
        {
            return context.GetDesignerValue(
                info,
                context.KnownTypes.ColorType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            );
        }

        return UseLibraryParseFunc(context, token, context.GetDesignerValue(info));
    }

    protected override Result<string> RenderScalar(
        IComponentContext context,
        CXValueGeneratorTarget target,
        CXToken token,
        CXValueGeneratorOptions options
    ) => FromText(context, token, token.Value);

    protected override Result<string> RenderMultipart(
        IComponentContext context,
        CXValueGeneratorTarget target,
        CXValue.Multipart multipart,
        CXValueGeneratorOptions options
    ) => UseLibraryParseFunc(
        context,
        multipart,
        StringGenerator.ToCSharpString(multipart)
    );

    private static Result<string> FromText(IComponentContext context, ICXNode owner, string text)
    {
        if (TryGetColorPreset(context, text, out var preset)) return preset;

        // check hex
        var hex = text;

        if (hex.StartsWith("#"))
            hex = hex.Substring(1);

        if (
            uint.TryParse(
                hex,
                NumberStyles.HexNumber,
                null,
                out var hexCode
            )
        )
        {
            return $"new {
                context.KnownTypes.ColorType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            }({hexCode})";
        }

        return UseLibraryParseFunc(context, owner, text);
    }

    private static Result<string> UseLibraryParseFunc(IComponentContext context, ICXNode owner, string text)
        => Result<string>.FromValue(
            $"{context.KnownTypes.ColorType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.Parse({
                StringGenerator.ToCSharpString(text)
            })",
            Diagnostics.FallbackToRuntimeValueParsing("Discord.Color.Parse"),
            owner
        );

    private static bool TryGetColorPreset(IComponentContext context, string text,
        [MaybeNullWhen(false)] out string preset)
    {
        if (
            TryGetColorPresetMap(context, out var map) &&
            map.TryGetValue(text.ToLowerInvariant(), out var name)
        )
        {
            preset =
                $"{context.KnownTypes.ColorType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{name}";
            return true;
        }

        preset = null;
        return false;
    }

    private static bool TryGetColorPresetMap(
        IComponentContext context,
        [MaybeNullWhen(false)] out IReadOnlyDictionary<string, string> map
    )
    {
        var colorType = context.Compilation.GetKnownTypes().ColorType;

        if (colorType is null)
        {
            map = null;
            return false;
        }

        var asmKey = colorType.ContainingAssembly.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        if (_fieldMaps.TryGetValue(asmKey, out map)) return true;

        map = _fieldMaps[asmKey] = colorType
            .GetMembers()
            .OfType<IFieldSymbol>()
            .Where(x =>
                x.Type.Equals(colorType, SymbolEqualityComparer.Default) &&
                x.IsStatic
            )
            .ToDictionary(x => x.Name.ToLowerInvariant(), x => x.Name);

        return true;
    }
}