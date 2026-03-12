using ComponentDesigner.Nodes;
using ComponentDesigner.Util;

namespace ComponentDesigner;

public sealed class NumericGenerator<TNumber> : CSharpValueGenerator
    where TNumber : struct
{
    public delegate bool Parser(string str, out TNumber result);

    private readonly bool _allowNullable;
    private readonly Parser _parser;

    private NumericGenerator(
        bool allowNullable,
        Parser parser
    )
    {
        _allowNullable = allowNullable;
        _parser = parser;
    }
    
    public static NumericGenerator<TNumber> Get(
        bool allowNullable,
        Parser parser
    ) => WeakMemoize.Of(
        allowNullable,
        a => new NumericGenerator<TNumber>(a, parser)
    );

    private static ICSharpTypeSymbol? GetSymbol(IRendererContext context)
    {
        if (typeof(TNumber) == typeof(byte)) return context.CompilationProvider.UInt8;
        if (typeof(TNumber) == typeof(sbyte)) return context.CompilationProvider.Int8;
        if (typeof(TNumber) == typeof(ushort)) return context.CompilationProvider.UInt16;
        if (typeof(TNumber) == typeof(short)) return context.CompilationProvider.Int16;
        if (typeof(TNumber) == typeof(uint)) return context.CompilationProvider.UInt32;
        if (typeof(TNumber) == typeof(int)) return context.CompilationProvider.Int32;
        if (typeof(TNumber) == typeof(ulong)) return context.CompilationProvider.UInt64;
        if (typeof(TNumber) == typeof(long)) return context.CompilationProvider.Int64;

        return null;
    }
    
    protected override Result<string> RenderLiteral(
        IRendererContext context,
        ComponentPropertyValue.Literal literalValue,
        string literal,
        CancellationToken cancellationToken = default
    ) => FromText(literal.SourcedAt(literalValue));

    protected override Result<string> RenderInterpolation(
        IRendererContext context,
        ComponentPropertyValue.Interpolation interpolationValue,
        IInterpolationInfo interpolationInfo,
        CancellationToken cancellationToken = default
    )
    {
        if (
            interpolationInfo.ConstantValue is { IsSpecified: true, Value: { } constant } &&
            _parser(constant.ToString(), out var number)
        )
        {
            return number.ToString();
        }

        var symbol = GetSymbol(context);
        
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
            return context.GetReferenceToDesignerValue(interpolationInfo, interpolationInfo.Symbol);
        }

        return Diagnostic
            .TypeMismatch(
                symbol?.Name ?? typeof(TNumber).Name,
                interpolationInfo.Symbol!
            )
            .At(interpolationValue);
    }

    private Result<string> FromText(
        SourcedValue<string> text
    )
    {
        if (_parser(text, out var number)) return number.ToString();

        return Diagnostic
            .TypeMismatch(typeof(TNumber).Name, "string")
            .At(text);
    }
}