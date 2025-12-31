using Discord.CX.Parser;
using Microsoft.CodeAnalysis;

namespace Discord.CX.Nodes;

public sealed class IntegerGenerator : CXValueGenerator
{
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
    )
    {
        return Result<string>.FromValue(
            $"int.Parse({StringGenerator.ToCSharpString(multipart)})",
            Diagnostics.FallbackToRuntimeValueParsing("int.Parse"),
            multipart
        );
    }

    protected override Result<string> RenderInterpolation(
        IComponentContext context,
        CXValueGeneratorTarget target,
        CXToken token,
        DesignerInterpolationInfo info,
        CXValueGeneratorOptions options)
    {
        if (
            info.Constant.HasValue &&
            (
                info.Constant.Value is int ||
                int.TryParse(info.Constant.Value?.ToString(), out _)
            )
        ) return info.Constant.Value!.ToString();

        if (
            context.Compilation.HasImplicitConversion(
                info.Symbol,
                context.Compilation.GetSpecialType(SpecialType.System_Int32)
            )
        )
        {
            return context.GetDesignerValue(info, "int");
        }

        return Result<string>.FromValue(
            $"int.Parse({context.GetDesignerValue(info)})",
            Diagnostics.FallbackToRuntimeValueParsing("int.Parse"),
            token
        );
    }

    private static Result<string> FromText(ICXNode owner, string text)
    {
        if (int.TryParse(text, out _)) return text;

        return Result<string>.FromValue(
            $"int.Parse({StringGenerator.ToCSharpString(text)})",
            Diagnostics.FallbackToRuntimeValueParsing("int.Parse"),
            owner
        );
    }
}