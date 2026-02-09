using ComponentDesigner.Nodes;
using ComponentDesigner.Parser;
using ComponentDesigner.Parser.Util;
using ComponentDesigner.Util;

namespace ComponentDesigner;

public sealed class BooleanGenerator : CSharpValueGenerator
{
    private readonly bool _allowNullable;

    private BooleanGenerator(bool allowNullable)
    {
        _allowNullable = allowNullable;
    }

    public static BooleanGenerator Get(bool allowNullable)
        => WeakMemoize.Of(allowNullable, static a => new BooleanGenerator(a));

    protected override Result<string> RenderInterpolation(
        IRendererContext context,
        CSharpValueGeneratorTarget target,
        CXToken token,
        IInterpolationInfo info,
        CSharpValueGeneratorOptions options,
        CancellationToken cancellationToken = default
    )
    {
        if (
            info.ConstantValue.TryGetOfType(out bool v)
        ) return v ? "true" : "false";

        if (info.ConstantValue.TryGetOfType(out string? str) && str is not null)
            return FromText(token, str);

        if (
            context.CompilationProvider.HasImplicitConversionBetween(
                info.Symbol,
                context.CompilationProvider.Boolean
            )
            ||
            (
                _allowNullable &&
                info.Symbol is not null &&
                info.Symbol.IsNullableTypeOf(
                    context.CompilationProvider.Boolean
                )
            )
        )
        {
            return context.GetReferenceToDesignerValue(info, info.Symbol);
        }

        string code;

        if (_allowNullable && info.Symbol.CanNullPatternMatch)
        {
            var varName = context.CreateVariable();
            
            code =
                $$"""
                  {{context.GetReferenceToDesignerValue(info, info.Symbol)}} is {} {{varName}}
                      ? bool.Parse({{varName}}.ToString())
                      : null
                  """;
        }
        else
        {
            code = $"bool.Parse({context.GetReferenceToDesignerValue(info)})";
        }


        return Result<string>.FromValue(
            code,
            token.Report(Diagnostic.UsingRuntimeValidation("bool.Parse"))
        );
    }

    protected override Result<string> RenderScalar(
        IRendererContext context,
        CSharpValueGeneratorTarget target,
        CXToken token,
        CSharpValueGeneratorOptions options,
        CancellationToken cancellationToken = default
    ) => FromText(token, token.Value);

    protected override Result<string> RenderMultipart(
        IRendererContext context,
        CSharpValueGeneratorTarget target,
        CXValue.Multipart multipart,
        CSharpValueGeneratorOptions options,
        CancellationToken cancellationToken = default
    )
    {
        return Result<string>.FromValue(
            $"bool.Parse({StringGenerator.ToCSharpString(multipart)})",
            multipart.Report(Diagnostic.UsingRuntimeValidation("bool.Parse"))
        );
    }

    protected override Result<string> RenderMissingValue(
        IRendererContext context,
        CSharpValueGeneratorTarget target,
        CSharpValueGeneratorOptions options,
        CancellationToken cancellationToken = default
    )
    {
        if (
            target is CSharpValueGeneratorTarget.ComponentProperty { PropertyValue.Property.RequiresValue: false }
        ) return "true";

        return base.RenderMissingValue(context, target, options, cancellationToken);
    }

    private static Result<string> FromText(ICXNode owner, string text)
    {
        var lower = text.ToLowerInvariant();

        if (lower is not "true" and not "false")
            return owner.Report(Diagnostic.TypeMismatch("bool", "string"));

        return lower;
    }
}