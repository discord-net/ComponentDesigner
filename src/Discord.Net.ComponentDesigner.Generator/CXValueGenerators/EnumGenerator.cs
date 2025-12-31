using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Discord.CX.Parser;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Discord.CX.Nodes;

public sealed class EnumGenerator : CXValueGenerator
{
    private readonly record struct RendererKey(string Name, bool RenderAsSymbolReference);

    private readonly record struct FieldMapKey(string Name, string Assembly);

    private readonly record struct EnumFieldInfo(string Name, Optional<object> Constant);

    private sealed record EnumInfo(
        string Name,
        string FullyQualifiedName,
        string? BaseType,
        string? QualifiedBaseType,
        IReadOnlyDictionary<string, EnumFieldInfo> Fields
    );

    private static readonly Dictionary<RendererKey, EnumGenerator> _renderers = [];
    private static readonly Dictionary<FieldMapKey, EnumInfo> _enumInfos = [];

    public string QualifiedName { get; }
    public bool RenderAsSymbolReference { get; }

    public EnumGenerator(
        string qualifiedName,
        bool renderAsSymbolReference
    )
    {
        QualifiedName = qualifiedName;
        RenderAsSymbolReference = renderAsSymbolReference;
    }

    public static EnumGenerator Create(
        string qualifiedEnumName,
        bool renderAsSymbolReference = true
    )
    {
        var key = new RendererKey(qualifiedEnumName, renderAsSymbolReference);

        if (_renderers.TryGetValue(key, out var renderer)) return renderer;

        return _renderers[key] = new(qualifiedEnumName, renderAsSymbolReference);
    }

    protected override Result<string> RenderScalar(
        IComponentContext context,
        CXValueGeneratorTarget target,
        CXToken token,
        CXValueGeneratorOptions options
    ) => FromText(context, target.Span, token.Value);

    protected override Result<string> RenderInterpolation(
        IComponentContext context,
        CXValueGeneratorTarget target,
        CXToken token,
        DesignerInterpolationInfo info,
        CXValueGeneratorOptions options
    )
    {
        if (
            !TryGetEnumInfo(context, QualifiedName, out var enumInfo) ||
            context.Compilation.GetTypeByMetadataName(QualifiedName) is not { } enumSymbol
        )
        {
            return UseEnumParseMethod(
                target.Span,
                context.GetDesignerValue(info)
            );
        }

        if (info.Constant.HasValue)
        {
            if (
                enumInfo.BaseType is not null &&
                context.Compilation.GetTypeByMetadataName(enumInfo.BaseType) is { } baseSymbol &&
                context.Compilation.HasImplicitConversion(
                    info.Symbol,
                    baseSymbol
                )
            )
            {
                var constStr = info.Constant.Value is string str
                    ? StringGenerator.ToCSharpString(str)
                    : info.Constant.ToString();

                return $"({(
                    RenderAsSymbolReference
                        ? enumInfo.FullyQualifiedName
                        : enumInfo.QualifiedBaseType
                )}){constStr}";
            }

            if (info.Constant.Value is string constantValue)
                return FromText(
                    context,
                    target.Span,
                    constantValue,
                    enumInfo
                );
        }

        if (
            context.Compilation.HasImplicitConversion(
                info.Symbol,
                enumSymbol
            )
        )
        {
            var designer = context.GetDesignerValue(
                info,
                enumInfo.FullyQualifiedName
            );

            if (RenderAsSymbolReference || enumInfo.QualifiedBaseType is null)
            {
                return designer;
            }

            return $"({enumInfo.QualifiedBaseType}){designer}";
        }

        return UseEnumParseMethod(
            target.Span,
            context.GetDesignerValue(info)
        );
    }

    protected override Result<string> RenderMultipart(
        IComponentContext context,
        CXValueGeneratorTarget target,
        CXValue.Multipart multipart,
        CXValueGeneratorOptions options
    ) => UseEnumParseMethod(
        target.Span,
        StringGenerator.ToCSharpString(multipart)
    );

    private Result<string> FromText(
        IComponentContext context,
        TextSpan span,
        string text,
        EnumInfo? info = null
    )
    {
        if (info is null)
        {
            TryGetEnumInfo(context, QualifiedName, out info);
        }
        
        if (info is not null && info.Fields.TryGetValue(text.ToLowerInvariant(), out var field))
            return RenderField(info, field);

        if (info is not null)
            return new DiagnosticInfo(
                Diagnostics.InvalidEnumVariant(
                    text,
                    info.Name
                ),
                span
            );

        return UseEnumParseMethod(span, StringGenerator.ToCSharpString(text));
    }

    private Result<string> RenderField(EnumInfo info, EnumFieldInfo field)
    {
        var enumRef = $"{info.FullyQualifiedName}.{field.Name}";

        if (RenderAsSymbolReference)
            return enumRef;

        if (field.Constant.HasValue)
        {
            if (info.QualifiedBaseType is not null)
                return $"({info.QualifiedBaseType}){field.Constant.Value}";

            return field.Constant.Value.ToString();
        }

        if (info.QualifiedBaseType is not null)
            return $"({info.QualifiedBaseType}){enumRef}";

        return enumRef;
    }

    private Result<string> UseEnumParseMethod(
        TextSpan span,
        string code
    ) => Result<string>.FromValue(
        $"global::System.Enum.Parse<{QualifiedName}>({code})",
        Diagnostics.FallbackToRuntimeValueParsing("Enum.Parse"),
        span
    );

    private static bool TryGetEnumInfo(
        IComponentContext context,
        string name,
        [MaybeNullWhen(false)] out EnumInfo info
    )
    {
        var symbol = context.Compilation.GetTypeByMetadataName(name);

        if (symbol is not INamedTypeSymbol { TypeKind: TypeKind.Enum })
        {
            info = null;
            return false;
        }

        var asmKey = symbol.ContainingAssembly.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        var key = new FieldMapKey(name, asmKey);

        if (_enumInfos.TryGetValue(key, out info)) return true;

        info = _enumInfos[key] = new(
            name,
            symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            symbol.EnumUnderlyingType?.ToDisplayString(),
            symbol.EnumUnderlyingType?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            symbol
                .GetMembers()
                .OfType<IFieldSymbol>()
                .Where(x =>
                    x.Type.Equals(symbol, SymbolEqualityComparer.Default) &&
                    x.IsStatic
                )
                .ToDictionary(
                    x => x.Name.ToLowerInvariant(),
                    x => new EnumFieldInfo(
                        x.Name,
                        x.HasConstantValue ? new(x.ConstantValue) : default
                    )
                )
        );

        return true;
    }
}