using ComponentDesigner.Parser;
using ComponentDesigner.Util;

namespace ComponentDesigner;

public sealed class SnowflakeGenerator : CSharpValueGenerator
{
    private readonly bool _allowNullable;

    private SnowflakeGenerator(bool allowNullable)
    {
        _allowNullable = allowNullable;
    }

    public static SnowflakeGenerator Get(bool allowNullable)
        => Memoize.Of(allowNullable, static a => new SnowflakeGenerator(a));

    protected override Result<string> RenderInterpolation(
        IRendererContext context,
        CSharpValueGeneratorTarget target,
        CXToken token,
        IInterpolationInfo info,
        CSharpValueGeneratorOptions options,
        CancellationToken cancellationToken = default)
    {
        if (info.ConstantValue.IsSpecified)
        {
            if (info.ConstantValue.Value is null)
            {
                if (_allowNullable) return "null";

                return token.Report(
                    Diagnostic.NullValueNotAllowed
                );
            }

            if (ulong.TryParse(info.ConstantValue.ToString(), out var ul))
                return ul.ToString();
        }

        if (
            context.CompilationProvider.HasImplicitConversionBetween(
                info.Symbol,
                context.CompilationProvider.UInt64,
                cancellationToken
            )
            ||
            (
                _allowNullable &&
                info.Symbol.IsNullableTypeOf(context.CompilationProvider.UInt64)
            )
        )
        {
            return context.GetReferenceToDesignerValue(info, info.Symbol);
        }

        return token.Report(
            Diagnostic.InvalidSnowflake(info.Symbol!.ToString())
        );
    }

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
    ) => multipart.Report(
        Diagnostic.InvalidSnowflake("<multipart>")
    );

    private static Result<string> FromText(CXTextSpan textSpan, string text)
    {
        if (ulong.TryParse(text, out _)) return text;

        return textSpan.Report(
            Diagnostic.InvalidSnowflake(text)
        );
    }
}