using ComponentDesigner.Nodes;
using ComponentDesigner.Util;

namespace ComponentDesigner.CSharp;

public sealed class NumericGenerator<TNumber> : CSharpValueGenerator
    where TNumber : struct
{
    public delegate bool Parser(string str, out TNumber result);

    private readonly bool _allowNullable;
    private readonly Parser _parser;
    private readonly StaticTypeSymbolFactory<CXTextSpan> _symbolFactory;

    private NumericGenerator(
        bool allowNullable,
        Parser parser
    )
    {
        _allowNullable = allowNullable;
        _parser = parser;

        _symbolFactory = GetSymbolFactory() ??
                         throw new InvalidOperationException($"{typeof(TNumber)} is not a valid numeric type");
    }

    private static StaticTypeSymbolFactory<CXTextSpan>? GetSymbolFactory()
    {
        if (typeof(TNumber) == typeof(byte)) return CompilationProviderExtension.UInt8;
        if (typeof(TNumber) == typeof(sbyte)) return CompilationProviderExtension.Int8;
        if (typeof(TNumber) == typeof(ushort)) return CompilationProviderExtension.UInt16;
        if (typeof(TNumber) == typeof(short)) return CompilationProviderExtension.Int16;
        if (typeof(TNumber) == typeof(uint)) return CompilationProviderExtension.UInt32;
        if (typeof(TNumber) == typeof(int)) return CompilationProviderExtension.Int32;
        if (typeof(TNumber) == typeof(ulong)) return CompilationProviderExtension.UInt64;
        if (typeof(TNumber) == typeof(long)) return CompilationProviderExtension.Int64;

        return null;
    }

    public static NumericGenerator<TNumber> Get(
        bool allowNullable,
        Parser parser
    ) => WeakMemoize.Of(
        allowNullable,
        a => new NumericGenerator<TNumber>(a, parser)
    );

    protected override Result<CSharpRender> RenderLiteral(
        IRenderContext context,
        ComponentPropertyValue.Literal literalValue,
        string literal,
        CancellationToken cancellationToken = default
    ) => _symbolFactory(context.CompilationProvider, literalValue.TextSpan, cancellationToken)
        .Map(symbol => FromText(symbol, literal.SourcedAt(literalValue)));

    protected override Result<CSharpRender> RenderInterpolation(
        IRenderContext context,
        ComponentPropertyValue.Interpolation interpolationValue,
        IInterpolationInfo interpolationInfo,
        CancellationToken cancellationToken = default
    ) => _symbolFactory(context.CompilationProvider, interpolationInfo.TextSpan, cancellationToken)
        .Map(Result<CSharpRender> (symbol) =>
        {
            if (
                interpolationInfo.ConstantValue is { IsSpecified: true, Value: { } constant } &&
                _parser(constant.ToString(), out var number)
            )
            {
                return new CSharpRender(
                    interpolationInfo.TextSpan,
                    number.ToString(),
                    symbol
                );
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
                    symbol?.Name ?? typeof(TNumber).Name,
                    interpolationInfo.Symbol!
                )
                .At(interpolationValue);
        });

    private Result<CSharpRender> FromText(
        ICSharpTypeSymbol symbol,
        SourcedValue<string> text
    )
    {
        if (_parser(text, out var number))
            return new CSharpRender(
                text.TextSpan,
                number.ToString(),
                symbol
            );

        return Diagnostic
            .TypeMismatch(typeof(TNumber).Name, "string")
            .At(text);
    }
}