using ComponentDesigner.Nodes;
using ComponentDesigner.Parser;
using ComponentDesigner.Parser.Util;
using ComponentDesigner.Util;

namespace ComponentDesigner.CSharp;

public sealed class BooleanGenerator : CSharpValueGenerator
{
    private readonly bool _allowNullable;

    private BooleanGenerator(bool allowNullable)
    {
        _allowNullable = allowNullable;
    }

    public static BooleanGenerator Get(bool allowNullable)
        => WeakMemoize.Of(allowNullable, static a => new BooleanGenerator(a));

    protected override Result<CSharpRender> RenderInterpolation(
        IRenderContext context,
        ComponentPropertyValue.Interpolation interpolationValue,
        IInterpolationInfo interpolationInfo,
        CancellationToken cancellationToken = default
    ) => context
        .CompilationProvider
        .Boolean(interpolationValue, cancellationToken)
        .Map(symbol =>
        {
            if (interpolationInfo.ConstantValue.TryGetOfType(out bool value))
                return new CSharpRender(
                    interpolationInfo.TextSpan,
                    value ? "true" : "false",
                    symbol
                );

            if (
                interpolationInfo.ConstantValue.TryGetOfType(out string? strValue) &&
                strValue is not null
            )
            {
                return FromText(symbol, strValue, interpolationInfo.TextSpan);
            }

            if (
                context.CompilationProvider.HasImplicitConversionBetween(
                    interpolationInfo.Symbol,
                    symbol,
                    cancellationToken
                )
                ||
                (
                    _allowNullable &&
                    interpolationInfo.Symbol.IsNullableTypeOf(symbol)
                )
            )
            {
                return new CSharpRender(
                    interpolationInfo.TextSpan,
                    context.GetReferenceToDesignerValue(interpolationInfo, interpolationInfo.Symbol),
                    interpolationInfo.Symbol
                );
            }

            return Diagnostic
                .TypeMismatch(
                    symbol,
                    interpolationInfo.Symbol!
                )
                .At(interpolationValue);
        });

    protected override Result<CSharpRender> RenderLiteral(
        IRenderContext context,
        ComponentPropertyValue.Literal literalValue,
        string literal,
        CancellationToken cancellationToken = default
    ) => context
        .CompilationProvider
        .Boolean(literalValue, cancellationToken)
        .Map(symbol => FromText(symbol, literalValue.Value, literalValue.TextSpan));

    protected override Result<CSharpRender> RenderNone(
        IRenderContext context,
        ComponentPropertyValue.None noneValue,
        CancellationToken cancellationToken = default
    ) => context
        .CompilationProvider
        .Boolean(noneValue, cancellationToken)
        .Map(symbol =>
        {
            if (
                noneValue is { IsAttributeNameOnly: true, Property.RequiresValue: false }
            )
            {
                return new CSharpRender(
                    noneValue.TextSpan,
                    "true",
                    symbol
                );
            }

            return base.RenderNone(context, noneValue, cancellationToken);
        });

    private static Result<CSharpRender> FromText(
        ICSharpTypeSymbol symbol,
        string text,
        CXTextSpan textSpan
    )
    {
        var lower = text.ToLowerInvariant();

        if (lower is not "true" and not "false")
            return Diagnostic.TypeMismatch("bool", "string").At(textSpan);

        return new CSharpRender(
            textSpan,
            lower,
            symbol
        );
    }
}