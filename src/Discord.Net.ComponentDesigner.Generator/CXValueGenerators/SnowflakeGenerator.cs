using System;
using Discord.CX.Parser;
using Microsoft.CodeAnalysis;

namespace Discord.CX.Nodes;

public sealed class SnowflakeGenerator : CXValueGenerator
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
            info.Constant.HasValue &&
            ulong.TryParse(info.Constant.Value?.ToString(), out var ul)
        ) return ul.ToString();

        if (
            context.Compilation.HasImplicitConversion(
                info.Symbol,
                context.Compilation.GetSpecialType(SpecialType.System_UInt64)
            )
        )
        {
            return context.GetDesignerValue(info, "ulong");
        }

        return UseParseMethod(token, context.GetDesignerValue(info, info.Symbol));
    }

    protected override Result<string> RenderScalar(
        IComponentContext context,
        CXValueGeneratorTarget target,
        CXToken token,
        CXValueGeneratorOptions options
    ) => FromText(token, token.Value);

    protected override Result<string> RenderMultipart(
        IComponentContext context,
        CXValueGeneratorTarget target,
        CXValue.Multipart multipart,
        CXValueGeneratorOptions options
    ) => UseParseMethod(multipart, StringGenerator.ToCSharpString(multipart));

    private static Result<string> FromText(ICXNode owner, string text)
    {
        if (ulong.TryParse(text, out _)) return text;

        return UseParseMethod(owner, StringGenerator.ToCSharpString(text));
    }

    private static Result<string> UseParseMethod(
        ICXNode owner,
        string code
    ) => Result<string>.FromValue(
        $"ulong.Parse({code})",
        Diagnostics.FallbackToRuntimeValueParsing("ulong.Parse"),
        owner
    );
}