using System;
using Discord.CX.Parser;
using Microsoft.CodeAnalysis;

namespace Discord.CX.Nodes;

public sealed class SnowflakeGenerator : CXValueGenerator
{
    private readonly bool _allowNullable;

    private SnowflakeGenerator(bool allowNullable)
    {
        _allowNullable = allowNullable;
    }

    public static SnowflakeGenerator Create(bool allowNullable)
        => Memoize.Of(allowNullable, static a => new SnowflakeGenerator(a));

    protected override Result<string> RenderInterpolation(
        IComponentContext context,
        CXValueGeneratorTarget target,
        CXToken token,
        DesignerInterpolationInfo info,
        CXValueGeneratorOptions options
    )
    {
        if (
            info.Constant.HasValue &&
            ulong.TryParse(info.Constant.Value?.ToString(), out var ul)
        ) return ul.ToString();

        if (_allowNullable && info.Constant is { HasValue: true, Value: null })
            return "null";

        if (
            context.Compilation.HasImplicitConversion(
                info.Symbol,
                context.Compilation.GetSpecialType(SpecialType.System_UInt64)
            )
            ||
            (
                _allowNullable &&
                info.Symbol.IsNullableOfValueType(
                    context.Compilation.GetSpecialType(SpecialType.System_UInt64),
                    context.Compilation
                )
            )
        )
        {
            return context.GetDesignerValue(info, info.Symbol);
        }

        return UseParseMethod(
            context,
            token,
            context.GetDesignerValue(info, info.Symbol),
            isNullable: info.Symbol.CanNullPatternMatch(context.Compilation)
        );
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
    ) => UseParseMethod(context, multipart, StringGenerator.ToCSharpString(multipart), isNullable: false);

    private Result<string> FromText(IComponentContext context, ICXNode owner, string text)
    {
        if (ulong.TryParse(text, out _)) return text;

        return UseParseMethod(context, owner, StringGenerator.ToCSharpString(text), isNullable: false);
    }

    private Result<string> UseParseMethod(
        IComponentContext context,
        ICXNode owner,
        string value,
        bool isNullable
    )
    {
        string code;

        if (_allowNullable && isNullable)
        {
            var varName = context.GetVariableName();
            code =
                $$"""
                  {{value}} is {} {{varName}}
                      ? ulong.Parse({{varName}}.ToString())
                      : null
                  """;
        }
        else
        {
            code = $"ulong.Parse({value})";
        }
        
        return Result<string>.FromValue(
            code,
            Diagnostics.FallbackToRuntimeValueParsing("ulong.Parse"),
            owner
        );
    }
}