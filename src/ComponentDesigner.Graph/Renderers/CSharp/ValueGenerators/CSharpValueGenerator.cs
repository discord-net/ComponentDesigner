using System.Diagnostics.CodeAnalysis;
using ComponentDesigner.CSharp;
using ComponentDesigner.Nodes;
using ComponentDesigner.Parser;

namespace ComponentDesigner.CSharp;

public abstract class CSharpValueGenerator
{
    public static CSharpValueGenerator UInt8 => NumericGenerator<byte>.Get(allowNullable: false, byte.TryParse);
    public static CSharpValueGenerator UInt16 => NumericGenerator<ushort>.Get(allowNullable: false, ushort.TryParse);
    public static CSharpValueGenerator UInt32 => NumericGenerator<uint>.Get(allowNullable: false, uint.TryParse);
    public static CSharpValueGenerator UInt64 => NumericGenerator<ulong>.Get(allowNullable: false, ulong.TryParse);
    public static CSharpValueGenerator Int8 => NumericGenerator<sbyte>.Get(allowNullable: false, sbyte.TryParse);
    public static CSharpValueGenerator Int16 => NumericGenerator<short>.Get(allowNullable: false, short.TryParse);
    public static CSharpValueGenerator Int32 => NumericGenerator<int>.Get(allowNullable: false, int.TryParse);
    public static CSharpValueGenerator Int64 => NumericGenerator<long>.Get(allowNullable: false, long.TryParse);
    
    public static CSharpValueGenerator NullableUInt8 => NumericGenerator<byte>.Get(allowNullable: true, byte.TryParse);
    public static CSharpValueGenerator NullableUInt16 => NumericGenerator<ushort>.Get(allowNullable: true, ushort.TryParse);
    public static CSharpValueGenerator NullableUInt32 => NumericGenerator<uint>.Get(allowNullable: true, uint.TryParse);
    public static CSharpValueGenerator NullableUInt64 => NumericGenerator<ulong>.Get(allowNullable: true, ulong.TryParse);
    public static CSharpValueGenerator NullableInt8 => NumericGenerator<sbyte>.Get(allowNullable: true, sbyte.TryParse);
    public static CSharpValueGenerator NullableInt16 => NumericGenerator<short>.Get(allowNullable: true, short.TryParse);
    public static CSharpValueGenerator NullableInt32 => NumericGenerator<int>.Get(allowNullable: true, int.TryParse);
    public static CSharpValueGenerator NullableInt64 => NumericGenerator<long>.Get(allowNullable: true, long.TryParse);

    public static CSharpValueGenerator Boolean => BooleanGenerator.Get(allowNullable: false);
    public static CSharpValueGenerator NullableBoolean => BooleanGenerator.Get(allowNullable: true);

    public static CSharpValueGenerator String => StringGenerator.Get(stringMode: StringNullMode.DisallowNull);
    public static CSharpValueGenerator NullableString => StringGenerator.Get(stringMode: StringNullMode.AllowNull);

    public static CSharpValueGenerator FromSymbol(
        ICompilationProvider compilationProvider,
        ICSharpTypeSymbol symbol,
        CancellationToken cancellationToken
    )
    {
        CSharpValueGenerator? result;
        
        if (symbol.TryUnwrapNullableValueType(out var inner))
        {
            TryGetCommonValueType(compilationProvider, inner, true, cancellationToken, out result);
        }
        else if(!TryGetCommonValueType(compilationProvider, symbol, false, cancellationToken, out result))
        {
            if (symbol.Equals(compilationProvider.String, cancellationToken))
                result = StringGenerator.Get(StringNullMode.TreatNullAsEmptyString);
        }
        
        return result ?? new InterpolationGenerator(symbol);
        
        static bool TryGetCommonValueType(
            ICompilationProvider compilation,
            ICSharpTypeSymbol symbol,
            bool nullable,
            CancellationToken cancellationToken,
            [MaybeNullWhen(false)] out CSharpValueGenerator result
        )
        {
            if (symbol.IsEnum)
                result = EnumGenerator.Get(symbol, renderAsSymbolReference: true, allowNullable: nullable);
            else if (symbol.Equals(compilation.Int8, cancellationToken))
                result = nullable ? NullableInt8 : Int8;
            else if (symbol.Equals(compilation.Int16, cancellationToken))
                result = nullable ? NullableInt16 : Int16;
            else if (symbol.Equals(compilation.Int32, cancellationToken))
                result = nullable ? NullableInt32 : Int32;
            else if (symbol.Equals(compilation.Int64, cancellationToken))
                result = nullable ? NullableInt64 : Int64;
            else if (symbol.Equals(compilation.UInt8, cancellationToken))
                result = nullable ? NullableUInt8 : UInt8;
            else if (symbol.Equals(compilation.UInt16, cancellationToken))
                result = nullable ? NullableUInt16 : UInt16;
            else if (symbol.Equals(compilation.UInt32, cancellationToken))
                result = nullable ? NullableUInt32 : UInt32;
            else if (symbol.Equals(compilation.UInt64, cancellationToken))
                result = nullable ? NullableUInt64 : UInt64;
            else if (symbol.Equals(compilation.Boolean, cancellationToken))
                result = BooleanGenerator.Get(allowNullable: nullable);
            else result = null;

            return result is not null;
        }
    }
    
    public virtual Result<CSharpRender> Render(
        IRenderContext context,
        ComponentPropertyValue value,
        CancellationToken cancellationToken = default
    ) => value switch
    {
        ComponentPropertyValue.Component component => RenderComponent(context, component, cancellationToken),
        ComponentPropertyValue.Interpolation interpolation => RenderInterpolation(context, interpolation, cancellationToken),
        ComponentPropertyValue.Literal literal => RenderLiteral(context, literal, cancellationToken),
        ComponentPropertyValue.Many many => RenderMany(context, many, cancellationToken),
        ComponentPropertyValue.None none => RenderNone(context, none, cancellationToken),
        _ => throw new ArgumentOutOfRangeException(nameof(value))
    };

    protected Result<CSharpRender> RenderComponent(
        IRenderContext context,
        ComponentPropertyValue.Component componentValue,
        CancellationToken cancellationToken = default
    ) => RenderComponent(
        context,
        componentValue,
        componentValue.GraphNode,
        componentValue.GraphNode.Component,
        componentValue.GraphNode.State,
        cancellationToken
    );

    protected virtual Result<CSharpRender> RenderComponent(
        IRenderContext context,
        ComponentPropertyValue.Component componentValue,
        GraphNode graphNode,
        IComponentNode component,
        ComponentState state,
        CancellationToken cancellationToken = default
    ) => Diagnostic
        .ValueVariantCannotBeGenerated(componentValue, this)
        .At(componentValue);

    protected Result<CSharpRender> RenderInterpolation(
        IRenderContext context,
        ComponentPropertyValue.Interpolation interpolationValue,
        CancellationToken cancellationToken = default
    ) => RenderInterpolation(context, interpolationValue, interpolationValue.Info, cancellationToken);
    
    protected virtual Result<CSharpRender> RenderInterpolation(
        IRenderContext context,
        ComponentPropertyValue.Interpolation interpolationValue,
        IInterpolationInfo interpolationInfo,
        CancellationToken cancellationToken = default
    ) => Diagnostic
        .ValueVariantCannotBeGenerated(interpolationValue, this)
        .At(interpolationValue);

    protected Result<CSharpRender> RenderLiteral(
        IRenderContext context,
        ComponentPropertyValue.Literal literalValue,
        CancellationToken cancellationToken = default
    ) => RenderLiteral(context, literalValue, literalValue.Value, cancellationToken);
    
    protected virtual Result<CSharpRender> RenderLiteral(
        IRenderContext context,
        ComponentPropertyValue.Literal literalValue,
        string literal,
        CancellationToken cancellationToken = default
    ) => Diagnostic
        .ValueVariantCannotBeGenerated(literalValue, this)
        .At(literalValue);

    protected Result<CSharpRender> RenderMany(
        IRenderContext context,
        ComponentPropertyValue.Many manyValue,
        CancellationToken cancellationToken = default
    ) => RenderMany(context, manyValue, manyValue.Values, cancellationToken);

    protected virtual Result<CSharpRender> RenderMany(
        IRenderContext context,
        ComponentPropertyValue.Many manyValue,
        IReadOnlyList<ComponentPropertyValue> values,
        CancellationToken cancellationToken = default
    )
    {
        if (values.Count is 1) return Render(context, values[0], cancellationToken);

        return Diagnostic
            .ValueVariantCannotBeGenerated(manyValue, this)
            .At(manyValue);
    }
    
    protected virtual Result<CSharpRender> RenderNone(
        IRenderContext context,
        ComponentPropertyValue.None noneValue,
        CancellationToken cancellationToken = default
    ) => Diagnostic
        .ValueVariantCannotBeGenerated(noneValue, this)
        .At(noneValue);
}