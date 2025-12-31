using Discord.CX.Parser;
using Microsoft.CodeAnalysis;

namespace Discord.CX.Nodes;

public sealed class BooleanGenerator : CXValueGenerator
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
            info.Constant is { HasValue: true, Value: bool v }
        ) return v ? "true" : "false";

        if (info.Constant is { HasValue: true, Value: string str })
            return FromText(token, str);

        if (
            context.Compilation.HasImplicitConversion(
                info.Symbol,
                context.Compilation.GetSpecialType(SpecialType.System_Boolean)
            )
        )
        {
            return context.GetDesignerValue(info, "bool");
        }


        return Result<string>.FromValue(
            $"bool.Parse({context.GetDesignerValue(info)})",
            Diagnostics.FallbackToRuntimeValueParsing("bool.Parse"),
            token
        );
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
    )
    {
        return Result<string>.FromValue(
            $"bool.Parse({StringGenerator.ToCSharpString(multipart)})",
            Diagnostics.FallbackToRuntimeValueParsing("bool.Parse"),
            multipart
        );
    }

    protected override Result<string> RenderMissingValue(
        IComponentContext context,
        CXValueGeneratorTarget target,
        CXValueGeneratorOptions options
    )
    {
        if (
            target is CXValueGeneratorTarget.ComponentProperty { Property: { RequiresValue: false } }
        ) return "true";
        
        return base.RenderMissingValue(context, target, options);
    }

    private static Result<string> FromText(ICXNode owner, string text)
    {
        var lower = text.ToLowerInvariant();
        if (lower is not "true" and not "false")
            return new DiagnosticInfo(
                Diagnostics.TypeMismatch("bool", "string"),
                owner
            );

        return lower;
    }
}