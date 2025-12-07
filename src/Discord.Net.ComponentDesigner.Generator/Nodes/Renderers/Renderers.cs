using Discord.CX.Parser;
using Microsoft.CodeAnalysis;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Discord.CX.Nodes.Components;

namespace Discord.CX.Nodes;

public static class Renderers
{
    public static PropertyRenderer CreateDefault(ComponentProperty property)
    {
        return (context, value) => { return string.Empty; };
    }

    public static PropertyRenderer CreateRenderer(ITypeSymbol type)
    {
        if (type.SpecialType == SpecialType.System_String)
            return Renderers.String;

        // TODO: more ways to extract renderers

        return (context, propValue) =>
        {
            switch (propValue.Value)
            {
                case CXValue.Interpolation interpolation:
                    var info = context.GetInterpolationInfo(interpolation);
                    if (
                        !context.Compilation.HasImplicitConversion(
                            info.Symbol,
                            type
                        )
                    )
                    {
                        context.AddDiagnostic(
                            Diagnostics.TypeMismatch,
                            interpolation,
                            info.Symbol,
                            type
                        );
                    }

                    return context.GetDesignerValue(
                        interpolation,
                        type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                    );
                default: return "default";
            }
        };
    }

    public static bool IsLoneInterpolatedLiteral(
        IComponentContext context,
        CXValue.Multipart literal,
        out DesignerInterpolationInfo info)
    {
        if (
            literal is { HasInterpolations: true, Tokens.Count: 1 } &&
            literal.Document.TryGetInterpolationIndex(literal.Tokens[0], out var index)
        )
        {
            info = context.GetInterpolationInfo(index);
            return true;
        }

        info = null!;
        return false;
    }

    public static PropertyRenderer ComponentAsProperty(ComponentRenderingOptions options)
        => (context, propertyValue) => ComponentAsProperty(context, propertyValue, options);

    public static string ComponentAsProperty(IComponentContext context, IComponentPropertyValue propertyValue)
        => ComponentAsProperty(context, propertyValue, ComponentRenderingOptions.Default);

    private static string ComponentAsProperty(
        IComponentContext context,
        IComponentPropertyValue propertyValue,
        ComponentRenderingOptions options
    )
    {
        switch (propertyValue.Value)
        {
            case CXValue.Element when propertyValue.Node is not null:
                return propertyValue.Node.Render(context, options);
            case CXValue.Interpolation interpolation:
                // ensure its a component builder
                var info = context.GetInterpolationInfo(interpolation);

                if (
                    !ComponentBuilderKindUtils.IsValidComponentBuilderType(
                        info.Symbol,
                        context.Compilation,
                        out var interleavedKind
                    )
                )
                {
                    context.AddDiagnostic(
                        Diagnostics.TypeMismatch,
                        interpolation,
                        info.Symbol,
                        $"{context.KnownTypes.IMessageComponentBuilderType} | {context.KnownTypes.MessageComponentType}"
                    );

                    return string.Empty;
                }

                var source = context.GetDesignerValue(
                    info,
                    info.Symbol!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                );

                // ensure we can convert it to a builder
                var target = options.TypingContext?.ConformingType ?? ComponentBuilderKind.IMessageComponentBuilder;
                
                if (
                    !ComponentBuilderKindUtils.TryConvert(
                        source,
                        interleavedKind,
                        target,
                        out var converted,
                        spreadCollections: options.TypingContext?.CanSplat is true
                    )
                )
                {
                    source = interleavedKind switch
                    {
                        ComponentBuilderKind.CXMessageComponent => $"{source}.Builders.First()",
                        ComponentBuilderKind.MessageComponent => $"{source}.Components.First().ToBuilder()",
                        ComponentBuilderKind.CollectionOfCXComponents => $"{source}.First().Builders.First()",
                        ComponentBuilderKind.CollectionOfIMessageComponentBuilders => $"{source}.First()",
                        ComponentBuilderKind.CollectionOfIMessageComponents => $"{source}.First().ToBuilder()",
                        ComponentBuilderKind.CollectionOfMessageComponents =>
                            $"{source}.First().Components.First().ToBuilder",
                        _ => string.Empty
                    };

                    if (source != string.Empty)
                    {
                        context.AddDiagnostic(
                            Diagnostics.CardinalityForcedToRuntime,
                            interpolation,
                            info.Symbol.ToDisplayString()
                        );
                    }
                    else
                    {
                        context.AddDiagnostic(
                            Diagnostics.InvalidChildComponentCardinality,
                            interpolation,
                            propertyValue.PropertyName
                        );
                    }
                }

                return source;
            default:
                if (propertyValue.Value is not null)
                {
                    context.AddDiagnostic(
                        Diagnostics.InvalidPropertyValueSyntax,
                        propertyValue.Value,
                        "interpolation"
                    );
                }

                return string.Empty;
        }
    }

    public static string UnfurledMediaItem(IComponentContext context, IComponentPropertyValue propertyValue)
        => $"new {
            context.KnownTypes
                .UnfurledMediaItemPropertiesType
                !.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}({String(context, propertyValue)
            })";

    public static string Integer(IComponentContext context, IComponentPropertyValue propertyValue)
    {
        switch (propertyValue.Value)
        {
            case CXValue.Scalar scalar:
                return FromText(scalar, scalar.Value);

            case CXValue.Interpolation interpolation:
                return FromInterpolation(interpolation, context.GetInterpolationInfo(interpolation));

            case CXValue.Multipart literal:
                if (!literal.HasInterpolations)
                    return FromText(literal, literal.Tokens.ToString().Trim());

                if (IsLoneInterpolatedLiteral(context, literal, out var info))
                    return FromInterpolation(literal, info);

                context.AddDiagnostic(
                    Diagnostics.FallbackToRuntimeValueParsing,
                    literal,
                    "int.Parse"
                );

                return $"int.Parse({RenderStringLiteral(literal)})";
            default: return "default";
        }

        string FromInterpolation(ICXNode owner, DesignerInterpolationInfo info)
        {
            if (info.Constant.Value is int || int.TryParse(info.Constant.Value?.ToString(), out _))
                return info.Constant.Value!.ToString();

            if (info.Constant.HasValue)
            {
                context.AddDiagnostic(
                    Diagnostics.TypeMismatch,
                    owner,
                    info.Constant.Value!.GetType().Name,
                    "int"
                );
            }

            if (
                context.Compilation.HasImplicitConversion(
                    info.Symbol,
                    context.Compilation.GetSpecialType(SpecialType.System_Int32)
                )
            )
            {
                return context.GetDesignerValue(info, "int");
            }

            context.AddDiagnostic(
                Diagnostics.FallbackToRuntimeValueParsing,
                owner,
                "int.Parse"
            );

            return $"int.Parse({context.GetDesignerValue(info)})";
        }

        string FromText(ICXNode owner, string text)
        {
            if (int.TryParse(text, out _)) return text;

            context.AddDiagnostic(
                Diagnostics.FallbackToRuntimeValueParsing,
                owner,
                "int.Parse"
            );

            return $"int.Parse({ToCSharpString(text)})";
        }
    }

    public static string Boolean(IComponentContext context, IComponentPropertyValue propertyValue)
    {
        switch (propertyValue.Value)
        {
            case CXValue.Interpolation interpolation:
                return FromInterpolation(interpolation, context.GetInterpolationInfo(interpolation));

            case CXValue.Scalar scalar:
                return FromText(scalar, scalar.Value.Trim().ToLowerInvariant());

            case CXValue.Multipart stringLiteral:
                if (!stringLiteral.HasInterpolations)
                    return FromText(stringLiteral, stringLiteral.Tokens.ToString().Trim().ToLowerInvariant());

                if (IsLoneInterpolatedLiteral(context, stringLiteral, out var info))
                    return FromInterpolation(stringLiteral, info);

                context.AddDiagnostic(
                    Diagnostics.FallbackToRuntimeValueParsing,
                    stringLiteral,
                    "bool.Parse"
                );

                return $"bool.Parse({context.GetDesignerValue(info)})";

            case null when !propertyValue.RequiresValue:
                return "true";

            default: return "default";
        }

        string FromInterpolation(ICXNode node, DesignerInterpolationInfo info)
        {
            if (info.Constant.Value is bool b) return b ? "true" : "false";


            if (
                context.Compilation.HasImplicitConversion(
                    info.Symbol,
                    context.Compilation.GetSpecialType(SpecialType.System_Boolean)
                )
            )
            {
                return context.GetDesignerValue(info, "bool");
            }

            if (info.Constant.Value?.ToString().Trim().ToLowerInvariant() is { } str and ("true" or "false"))
                return str;

            context.AddDiagnostic(
                Diagnostics.FallbackToRuntimeValueParsing,
                node,
                "bool.Parse"
            );

            return $"bool.Parse({context.GetDesignerValue(info)})";
        }

        string FromText(ICXNode owner, string value)
        {
            if (value is not "true" and not "false")
            {
                context.AddDiagnostic(
                    Diagnostics.TypeMismatch,
                    owner,
                    "string",
                    "bool"
                );
            }

            return value;
        }
    }

    private static readonly Dictionary<string, string> _colorPresets = [];

    private static bool TryGetColorPreset(
        IComponentContext context,
        string value,
        out string fieldName)
    {
        var colorSymbol = context.KnownTypes.ColorType;

        if (colorSymbol is null)
        {
            fieldName = null!;
            return false;
        }

        if (_colorPresets.Count is 0)
        {
            foreach (
                var field
                in colorSymbol.GetMembers()
                    .OfType<IFieldSymbol>()
                    .Where(x =>
                        x.Type.Equals(colorSymbol, SymbolEqualityComparer.Default) &&
                        x.IsStatic
                    )
            )
            {
                _colorPresets[field.Name.ToLowerInvariant()] = field.Name;
            }
        }

        return _colorPresets.TryGetValue(value.ToLowerInvariant(), out fieldName);
    }

    public static string Color(IComponentContext context, IComponentPropertyValue propertyValue)
    {
        var colorSymbol = context.KnownTypes.ColorType;
        var qualifiedColor = colorSymbol!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        switch (propertyValue.Value)
        {
            case CXValue.Interpolation interpolation:
                return FromInterpolation(interpolation, context.GetInterpolationInfo(interpolation));

            case CXValue.Scalar scalar:
                return FromText(scalar, scalar.Value);

            case CXValue.Multipart stringLiteral:

                if (!stringLiteral.HasInterpolations)
                    return FromText(stringLiteral, stringLiteral.Tokens.ToString());

                if (IsLoneInterpolatedLiteral(context, stringLiteral, out var info))
                    return FromInterpolation(stringLiteral, info);

                context.AddDiagnostic(
                    Diagnostics.FallbackToRuntimeValueParsing,
                    stringLiteral,
                    "Discord.Color.Parse"
                );

                return UseLibraryParser(RenderStringLiteral(stringLiteral));
            default: return "default";
        }

        string FromInterpolation(ICXNode owner, DesignerInterpolationInfo info)
        {
            if (
                info.Symbol is not null &&
                context.Compilation.HasImplicitConversion(
                    info.Symbol,
                    colorSymbol
                )
            )
            {
                return context.GetDesignerValue(
                    info,
                    qualifiedColor
                );
            }

            uint hexColor;

            if (info.Constant.Value is string str)
            {
                if (TryGetColorPreset(context, str, out var preset))
                    return $"{qualifiedColor}.{preset}";

                if (TryParseHexColor(str, out hexColor))
                    return $"new {qualifiedColor}({hexColor})";
            }
            else if (info.Constant.HasValue && uint.TryParse(info.Constant.Value?.ToString(), out hexColor))
            {
                return $"new {qualifiedColor}({hexColor})";
            }

            if (
                context.Compilation.HasImplicitConversion(
                    info.Symbol,
                    context.Compilation.GetSpecialType(SpecialType.System_UInt32)
                )
            )
            {
                return $"new {qualifiedColor}({context.GetDesignerValue(info, "uint")})";
            }

            context.AddDiagnostic(
                Diagnostics.FallbackToRuntimeValueParsing,
                owner,
                "Discord.Color.Parse"
            );

            return $"{qualifiedColor}.Parse({context.GetDesignerValue(info)})";
        }

        string FromText(ICXNode owner, string rawValue)
        {
            if (TryGetColorPreset(context, rawValue, out var presetName))
            {
                return $"{qualifiedColor}.{presetName}";
            }

            // maybe hex?
            var hex = rawValue;

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
                return $"new {qualifiedColor}({hexCode})";

            context.AddDiagnostic(
                Diagnostics.FallbackToRuntimeValueParsing,
                owner,
                "Discord.Color.Parse"
            );

            return UseLibraryParser(ToCSharpString(rawValue));
        }

        string UseLibraryParser(string source)
            => $"{qualifiedColor}.Parse({source})";

        static bool TryParseHexColor(string hexColor, out uint color)
        {
            if (string.IsNullOrWhiteSpace(hexColor))
            {
                color = 0;
                return false;
            }

            if (hexColor[0] is '#')
                hexColor = hexColor.Substring(1);
            else if (hexColor.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                hexColor = hexColor.Substring(2);

            return uint.TryParse(hexColor, NumberStyles.HexNumber, null, out color);
        }
    }

    public static string Snowflake(IComponentContext context, IComponentPropertyValue propertyValue)
        => Snowflake(context, propertyValue.Value);

    public static string Snowflake(IComponentContext context, CXValue? value)
    {
        switch (value)
        {
            case CXValue.Interpolation interpolation:
                return FromInterpolation(interpolation, context.GetInterpolationInfo(interpolation));

            case CXValue.Scalar scalar:
                return FromText(scalar, scalar.Value.Trim());

            case CXValue.Multipart stringLiteral:
                if (!stringLiteral.HasInterpolations)
                    return FromText(stringLiteral, stringLiteral.Tokens.ToString().Trim());

                if (IsLoneInterpolatedLiteral(context, stringLiteral, out var info))
                    return FromInterpolation(stringLiteral, info);

                context.AddDiagnostic(
                    Diagnostics.FallbackToRuntimeValueParsing,
                    stringLiteral,
                    "ulong.Parse"
                );

                return UseParseMethod(RenderStringLiteral(stringLiteral));

            default: return "default";
        }

        string FromInterpolation(ICXNode owner, DesignerInterpolationInfo info)
        {
            var targetType = context.Compilation.GetSpecialType(SpecialType.System_UInt64);

            if (info.Constant is { HasValue: true, Value: ulong ul })
                return ul.ToString();

            if (
                info.Symbol is not null &&
                context.Compilation.HasImplicitConversion(info.Symbol, targetType)
            )
            {
                return context.GetDesignerValue(info, "ulong");
            }

            context.AddDiagnostic(
                Diagnostics.FallbackToRuntimeValueParsing,
                owner,
                "ulong.Parse"
            );

            return UseParseMethod(context.GetDesignerValue(info));
        }

        string FromText(ICXNode owner, string text)
        {
            if (ulong.TryParse(text, out _)) return text;

            context.AddDiagnostic(
                Diagnostics.FallbackToRuntimeValueParsing,
                owner,
                "ulong.Parse"
            );

            return UseParseMethod(ToCSharpString(text));
        }

        static string UseParseMethod(string input)
            => $"ulong.Parse({input})";
    }

    public static string String(IComponentContext context, IComponentPropertyValue propertyValue)
    {
        switch (propertyValue.Value)
        {
            default: return "string.Empty";

            case CXValue.Interpolation interpolation:
                if (context.GetInterpolationInfo(interpolation).Constant.Value is string constant)
                    return ToCSharpString(constant);

                return context.GetDesignerValue(interpolation);
            case CXValue.Multipart literal: return RenderStringLiteral(literal);
            case CXValue.Scalar scalar:
                return ToCSharpString(scalar.FullValue);
        }
    }

    public static string RenderStringLiteral(CXValue.Multipart literal)
    {
        if (literal.Tokens.Count is 0) return "string.Empty";

        var sb = new StringBuilder();

        var parts = literal.Tokens
            .Where(x => x.Kind is CXTokenKind.Text)
            .Select(x => x.Value)
            .ToArray();

        if (parts.Length is 0) return string.Empty;

        parts[0] = parts[0].TrimStart();

        parts[parts.Length - 1] = parts[parts.Length - 1].TrimEnd();

        var quoteCount = parts.Select(x => x.Count(x => x is '"')).Max() + 1;

        var hasInterpolations = literal.Tokens.Any(x => x.Kind is CXTokenKind.Interpolation);

        var dollars = hasInterpolations
            ? new string(
                '$',
                Math.Max(1, parts.Select(GetInterpolationDollarRequirement).Max())
            )
            : string.Empty;

        var startInterpolation = dollars.Length > 0
            ? new string('{', dollars.Length)
            : string.Empty;

        var endInterpolation = dollars.Length > 0
            ? new string('}', dollars.Length)
            : string.Empty;

        var isMultiline = literal.Tokens.Any(x =>
            x.LeadingTrivia.ContainsNewlines ||
            x.TrailingTrivia.ContainsNewlines ||
            x.Value.Contains('\n')
        );

        foreach (var token in literal.Tokens)
        {
            switch (token.Kind)
            {
                case CXTokenKind.Text:
                    sb
                        .Append(token.LeadingTrivia)
                        .Append(EscapeBackslashes(token.Value))
                        .Append(token.TrailingTrivia);
                    break;
                case CXTokenKind.Interpolation:
                    var index = Array.IndexOf(literal.Document.InterpolationTokens, token);

                    // TODO: handle better
                    if (index is -1) throw new InvalidOperationException();

                    sb
                        .Append(token.LeadingTrivia)
                        .Append(startInterpolation)
                        .Append($"designer.GetValueAsString({index})")
                        .Append(endInterpolation)
                        .Append(token.TrailingTrivia);
                    break;

                default: continue;
            }
        }

        // normalize the value indentation
        var value = sb.ToString().NormalizeIndentation().Trim(['\r', '\n']);

        // pad the value to the amount of dollar signs we have to properly align the value text to the 
        // multi-line string literal
        if (hasInterpolations && isMultiline)
            value = value.Indent(dollars.Length);

        sb.Clear();

        if (isMultiline)
        {
            sb.AppendLine();
            quoteCount = Math.Max(quoteCount, 3);
        }

        var quotes = new string('"', quoteCount);

        sb.Append(dollars).Append(quotes);

        if (isMultiline) sb.AppendLine();

        sb.Append(value);

        // ending quotes are on a different line 
        if (isMultiline) sb.AppendLine();

        // if it has interpolations, offset the ending quotes by the amount of dollar signs
        if (hasInterpolations && isMultiline) sb.Append("".PadLeft(dollars.Length));
        sb.Append(quotes);

        return sb.ToString();
    }

    public static int GetInterpolationDollarRequirement(string part)
    {
        var result = 0;

        var count = 0;
        char? last = null;

        foreach (var ch in part)
        {
            if (ch is '{' or '}')
            {
                if (last is null)
                {
                    last = ch;
                    count = 1;
                    continue;
                }

                if (last == ch)
                {
                    count++;
                    continue;
                }
            }

            if (count > 0)
            {
                result = Math.Max(result, count);
                last = null;
                count = 0;
            }
        }

        return result;
    }

    public static string ToCSharpString(string text)
    {
        var quoteCount = (GetSequentialQuoteCount(text) + 1) switch
        {
            2 => 3,
            var r => r
        };

        text = text.NormalizeIndentation().Trim(['\r', '\n']);

        var isMultiline = text.Contains('\n');

        if (isMultiline)
            quoteCount = Math.Max(3, quoteCount);

        var quotes = new string('"', quoteCount);

        var sb = new StringBuilder();

        if (isMultiline) sb.AppendLine();

        sb.Append(quotes);

        if (isMultiline) sb.AppendLine();

        sb.Append(text);

        if (isMultiline)
            sb.AppendLine();

        sb.Append(quotes);

        return sb.ToString();
    }

    private static string EscapeBackslashes(string text)
        => text.Replace("\\", @"\\");

    public static int GetSequentialQuoteCount(string text)
    {
        var result = 0;
        var count = 0;

        foreach (var ch in text)
        {
            if (ch is '"')
            {
                count++;
                continue;
            }

            if (count > 0)
            {
                result = Math.Max(result, count);
                count = 0;
            }
        }

        return result;
    }

    public static PropertyRenderer RenderEnum(string fullyQualifiedName)
    {
        ITypeSymbol? symbol = null;
        Dictionary<string, string> variants = [];

        return (context, propertyValue) =>
        {
            if (symbol is null || variants.Count is 0)
            {
                symbol = context.Compilation.GetTypeByMetadataName(fullyQualifiedName);

                if (symbol is null) throw new InvalidOperationException($"Unknown type '{fullyQualifiedName}'");

                if (symbol.TypeKind is not TypeKind.Enum)
                    throw new InvalidOperationException($"'{symbol}' is not an enum type.");

                variants = symbol
                    .GetMembers()
                    .OfType<IFieldSymbol>()
                    .Where(x => x.Type.Equals(symbol, SymbolEqualityComparer.Default))
                    .ToDictionary(x => x.Name.ToLowerInvariant(), x => x.Name);
            }

            switch (propertyValue.Value)
            {
                case CXValue.Scalar scalar:
                    return FromText(scalar.Value.Trim(), scalar);
                case CXValue.Interpolation interpolation:
                    return FromInterpolation(interpolation, context.GetInterpolationInfo(interpolation));
                case CXValue.Multipart literal:
                    if (!literal.HasInterpolations)
                        return FromText(literal.Tokens.ToString().Trim().ToLowerInvariant(), literal);

                    if (IsLoneInterpolatedLiteral(context, literal, out var info))
                        return FromInterpolation(literal, info);

                    return
                        $"global::System.Enum.Parse<{symbol!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>({RenderStringLiteral(literal)})";
                default: return "default";
            }

            string FromInterpolation(ICXNode owner, DesignerInterpolationInfo info)
            {
                if (
                    context.Compilation.HasImplicitConversion(
                        info.Symbol,
                        symbol
                    )
                )
                {
                    return context.GetDesignerValue(
                        info,
                        symbol!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                    );
                }

                if (info.Constant.Value?.ToString() is { } str)
                {
                    return FromText(str.Trim().ToLowerInvariant(), owner);
                }

                return
                    $"global::System.Enum.Parse<{symbol!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>({context.GetDesignerValue(info)})";
            }

            string FromText(string text, ICXNode owner)
            {
                if (variants.TryGetValue(text, out var name))
                    return $"{symbol!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{name}";

                context.AddDiagnostic(
                    Diagnostics.InvalidEnumVariant,
                    owner,
                    text,
                    symbol.ToDisplayString()
                );

                return
                    $"global::System.Enum.Parse<{symbol!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>({ToCSharpString(text)})";
            }
        };
    }

    public static string Emoji(IComponentContext context, IComponentPropertyValue propertyValue)
    {
        bool isDiscordEmote = false;
        bool isEmoji = false;
        switch (propertyValue.Value)
        {
            case CXValue.Interpolation interpolation:
                var interpolationInfo = context.GetInterpolationInfo(interpolation);

                if (
                    interpolationInfo.Symbol is not null &&
                    context.Compilation.HasImplicitConversion(
                        interpolationInfo.Symbol,
                        context.KnownTypes.IEmoteType
                    )
                )
                {
                    return context.GetDesignerValue(
                        interpolation,
                        $"{context.KnownTypes.IEmoteType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}"
                    );
                }

                return UseLibraryParser(
                    context.GetDesignerValue(interpolation)
                );

            case CXValue.Scalar scalar:
                LightlyValidateEmote(scalar.Value, scalar, out isDiscordEmote, out isEmoji);
                return UseLibraryParser(ToCSharpString(scalar.Value), isEmoji, isDiscordEmote);

            case CXValue.Multipart stringLiteral:
                if (!stringLiteral.HasInterpolations)
                    LightlyValidateEmote(
                        stringLiteral.Tokens.ToString(),
                        stringLiteral.Tokens,
                        out isDiscordEmote,
                        out isEmoji
                    );

                return UseLibraryParser(RenderStringLiteral(stringLiteral), isEmoji, isDiscordEmote);

            default: return "null";
        }

        void LightlyValidateEmote(string emote, ICXNode node, out bool isDiscordEmote, out bool isEmoji)
        {
            isDiscordEmote = IsDiscordEmote.IsMatch(emote);
            isEmoji = IsEmoji.IsMatch(emote);

            if (!isDiscordEmote && !isEmoji)
            {
                context.AddDiagnostic(
                    Diagnostics.PossibleInvalidEmote,
                    node,
                    emote
                );
            }
        }

        static string UseLibraryParser(string source, bool? isEmoji = null, bool? isDiscordEmote = null)
        {
            if (isEmoji is true)
                return $"global::Discord.Emoji.Parse({source})";

            if (isDiscordEmote is true)
                return $"global::Discord.Emote.Parse({source})";

            return
                $"""
                 global::Discord.Emoji.TryParse({source}, out var emoji)
                     ? (global::Discord.IEmote)emoji
                     : global::Discord.Emote.Parse({source})
                 """;
        }
    }

    private static readonly Regex IsEmoji = new Regex(@"^(?>(?>[\uD800-\uDBFF][\uDC00-\uDFFF]\p{M}*){1,5}|\p{So})$",
        RegexOptions.Compiled);

    private static readonly Regex IsDiscordEmote = new Regex(@"^<(?>a|):.+:\d+>$", RegexOptions.Compiled);
}