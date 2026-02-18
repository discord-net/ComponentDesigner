using ComponentDesigner.Parser;
using ComponentDesigner.Parser.Util;
using ComponentDesigner.Util;

namespace ComponentDesigner;

public sealed class IntegerGenerator : CSharpValueGenerator
{
    private readonly bool _allowNullable;

    private IntegerGenerator(bool allowNullable)
    {
        _allowNullable = allowNullable;
    }

    public static IntegerGenerator Get(bool allowNullable)
        => WeakMemoize.Of(allowNullable, static a => new IntegerGenerator(a));

    protected override Result<string> RenderScalar(
        IRendererContext context,
        CSharpValueGeneratorTarget target,
        CXToken token,
        CSharpValueGeneratorOptions options,
        CancellationToken cancellationToken = default
    ) => FromText(token.TextSpan, token.Value);

    protected override Result<string> RenderMultipart(
        IRendererContext context,
        CSharpValueGeneratorTarget target,
        CXValue.Multipart multipart,
        CSharpValueGeneratorOptions options,
        CancellationToken cancellationToken = default
    ) => (
        $"int.Parse({StringGenerator.ToCSharpString(multipart)})",
        multipart.Report(Diagnostic.UsingRuntimeValidation("int.Parse"))
    );

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
            if (info.ConstantValue.Value is int i) return i.ToString();

            if (int.TryParse(info.ConstantValue.Value?.ToString(), out i)) return i.ToString();

            if (info.ConstantValue.Value is null)
            {
                if (_allowNullable) return "null";
                return token.Report(Diagnostic.NullValueNotAllowed);
            }
        }

        if (
            context.CompilationProvider.HasImplicitConversionBetween(
                info.Symbol,
                context.CompilationProvider.Int32
            )
            ||
            (
                _allowNullable &&
                info.Symbol.IsNullableTypeOf(context.CompilationProvider.Int32)
            )
        )
        {
            return context.GetReferenceToDesignerValue(
                info,
                info.Symbol
            );
        }

        string code;

        if (_allowNullable && info.Symbol.CanNullPatternMatch)
        {
            var varName = context.CreateVariable();
            code =
                $$"""
                  {{context.GetReferenceToDesignerValue(info, info.Symbol)}} is {} {{varName}}
                      ? int.Parse({{varName}}.ToString())
                      : null
                  """;
        }
        else
        {
            code = $"int.Parse({context.GetReferenceToDesignerValue(info)})";
        }

        return (
            code,
            token.Report(Diagnostic.UsingRuntimeValidation("int.Parse"))
        );
    }

    private static Result<string> FromText(CXTextSpan textSpan, string text)
    {
        if (int.TryParse(text, out _)) return text;

        return (
            $"int.Parse({StringGenerator.ToCSharpString(text)})",
            textSpan.Report(Diagnostic.UsingRuntimeValidation("int.Parse"))
        );
    }
}