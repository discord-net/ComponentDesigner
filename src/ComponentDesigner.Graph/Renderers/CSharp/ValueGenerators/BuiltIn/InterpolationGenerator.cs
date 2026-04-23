using ComponentDesigner.Nodes;
using ComponentDesigner.Parser;
using ComponentDesigner.Util;

namespace ComponentDesigner;

public sealed class InterpolationGenerator(ICSharpTypeSymbol symbol) : CSharpValueGenerator
{
    public static InterpolationGenerator Get(ICSharpTypeSymbol symbol)
        => WeakMemoize.Of(symbol, static (s) => new InterpolationGenerator(s));

    protected override Result<string> RenderInterpolation(
        IRenderContext context,
        ComponentPropertyValue.Interpolation interpolationValue,
        IInterpolationInfo interpolationInfo,
        CancellationToken cancellationToken = default
    )
    {
        if (
            !context.CompilationProvider.HasImplicitConversionBetween(
                interpolationInfo.Symbol,
                symbol,
                cancellationToken
            )
        )
        {
            return Diagnostic
                .TypeMismatch(
                    symbol,
                    interpolationInfo.Symbol!
                )
                .At(interpolationValue);
        }

        return context.GetReferenceToDesignerValue(interpolationInfo, interpolationInfo.Symbol);
    }
}