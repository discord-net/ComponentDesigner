using ComponentDesigner.Parser;
using ComponentDesigner.Util;

namespace ComponentDesigner;

public sealed class InterpolationGenerator(ICSharpTypeSymbol symbol) : CSharpValueGenerator
{
    public static InterpolationGenerator Get(ICSharpTypeSymbol symbol)
        => WeakMemoize.Of(symbol, static (s) => new InterpolationGenerator(s));
    
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
            !context.CompilationProvider.HasImplicitConversionBetween(
                info.Symbol,
                symbol,
                cancellationToken
            )
        )
        {
            return token.Report(
                Diagnostic.TypeMismatch(symbol, info.Symbol!)
            );
        }

        return context.GetReferenceToDesignerValue(info, symbol);
    }
}