using System.Diagnostics.CodeAnalysis;
using ComponentDesigner.Nodes;
using ComponentDesigner.Parser;

namespace ComponentDesigner;

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

    public static CSharpValueGenerator FromSymbol(
        ICompilationProvider compilationProvider,
        ICSharpTypeSymbol symbol
    )
    {
        CSharpValueGenerator? result;
        
        if (symbol.TryUnwrapNullableValueType(out var inner))
        {
            TryGetCommonValueType(compilationProvider, inner, true, out result);
        }
        else if(!TryGetCommonValueType(compilationProvider, symbol, false, out result))
        {
            if (symbol.Equals(compilationProvider.String!))
                result = StringGenerator.Get(StringNullMode.TreatNullAsEmptyString);
        }
        
        return result ?? new InterpolationGenerator(symbol);
        
        static bool TryGetCommonValueType(
            ICompilationProvider compilation,
            ICSharpTypeSymbol symbol,
            bool nullable,
            [MaybeNullWhen(false)] out CSharpValueGenerator result
        )
        {
            if (symbol.IsEnum)
                result = EnumGenerator.Get(symbol, renderAsSymbolReference: true, allowNullable: nullable);
            else if (symbol.Equals(compilation.Int8!))
                result = nullable ? NullableInt8 : Int8;
            else if (symbol.Equals(compilation.Int16!))
                result = nullable ? NullableInt16 : Int16;
            else if (symbol.Equals(compilation.Int32!))
                result = nullable ? NullableInt32 : Int32;
            else if (symbol.Equals(compilation.Int64!))
                result = nullable ? NullableInt64 : Int64;
            else if (symbol.Equals(compilation.UInt8!))
                result = nullable ? NullableUInt8 : UInt8;
            else if (symbol.Equals(compilation.UInt16!))
                result = nullable ? NullableUInt16 : UInt16;
            else if (symbol.Equals(compilation.UInt32!))
                result = nullable ? NullableUInt32 : UInt32;
            else if (symbol.Equals(compilation.UInt64!))
                result = nullable ? NullableUInt64 : UInt64;
            else if (symbol.Equals(compilation.Boolean!))
                result = BooleanGenerator.Get(allowNullable: nullable);
            else result = null;

            return result is not null;
        }
    }
    
    public virtual Result<string> Render(
        IRendererContext context,
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

    protected Result<string> RenderComponent(
        IRendererContext context,
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

    protected virtual Result<string> RenderComponent(
        IRendererContext context,
        ComponentPropertyValue.Component componentValue,
        GraphNode graphNode,
        IComponentNode component,
        ComponentState state,
        CancellationToken cancellationToken = default
    ) => Diagnostic
        .ValueVariantCannotBeGenerated(componentValue, this)
        .At(componentValue);

    protected Result<string> RenderInterpolation(
        IRendererContext context,
        ComponentPropertyValue.Interpolation interpolationValue,
        CancellationToken cancellationToken = default
    ) => RenderInterpolation(context, interpolationValue, interpolationValue.Info, cancellationToken);
    
    protected virtual Result<string> RenderInterpolation(
        IRendererContext context,
        ComponentPropertyValue.Interpolation interpolationValue,
        IInterpolationInfo interpolationInfo,
        CancellationToken cancellationToken = default
    ) => Diagnostic
        .ValueVariantCannotBeGenerated(interpolationValue, this)
        .At(interpolationValue);

    protected Result<string> RenderLiteral(
        IRendererContext context,
        ComponentPropertyValue.Literal literalValue,
        CancellationToken cancellationToken = default
    ) => RenderLiteral(context, literalValue, literalValue.Value, cancellationToken);
    
    protected virtual Result<string> RenderLiteral(
        IRendererContext context,
        ComponentPropertyValue.Literal literalValue,
        string literal,
        CancellationToken cancellationToken = default
    ) => Diagnostic
        .ValueVariantCannotBeGenerated(literalValue, this)
        .At(literalValue);

    protected Result<string> RenderMany(
        IRendererContext context,
        ComponentPropertyValue.Many manyValue,
        CancellationToken cancellationToken = default
    ) => RenderMany(context, manyValue, manyValue.Values, cancellationToken);

    protected virtual Result<string> RenderMany(
        IRendererContext context,
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
    
    protected virtual Result<string> RenderNone(
        IRendererContext context,
        ComponentPropertyValue.None noneValue,
        CancellationToken cancellationToken = default
    ) => Diagnostic
        .ValueVariantCannotBeGenerated(noneValue, this)
        .At(noneValue);
}

// public abstract class CSharpValueGenerator
// {
//     public static CSharpValueGenerator Boolean => BooleanGenerator.Get(allowNullable: false);
//     public static CSharpValueGenerator NullableBoolean => BooleanGenerator.Get(allowNullable: true);
//     public static CSharpValueGenerator Integer => IntegerGenerator.Get(allowNullable: false);
//     public static CSharpValueGenerator NullableInteger => IntegerGenerator.Get(allowNullable: true);
//     public static CSharpValueGenerator Snowflake => SnowflakeGenerator.Get(allowNullable: false);
//     public static CSharpValueGenerator NullableSnowflake => SnowflakeGenerator.Get(allowNullable: true);
//     public static CSharpValueGenerator String => StringGenerator.Get(StringNullMode.DisallowNull);
//     public static CSharpValueGenerator NullableString => StringGenerator.Get(StringNullMode.AllowNull);
//     
//     public static CSharpValueGenerator FromSymbol(
//         ICompilationProvider compilationProvider,
//         ICSharpTypeSymbol symbol
//     )
//     {
//         CSharpValueGenerator? result;
//
//         if (symbol.TryUnwrapNullableValueType(out var inner))
//         {
//             TryGetCommonValueType(compilationProvider, inner, true, out result);
//         }
//         else if(!TryGetCommonValueType(compilationProvider, symbol, false, out result))
//         {
//             if (symbol.Equals(compilationProvider.String!))
//                 result = StringGenerator.Get(StringNullMode.TreatNullAsEmptyString);
//         }
//         
//         return result ?? new InterpolationGenerator(symbol);
//         
//         static bool TryGetCommonValueType(
//             ICompilationProvider compilation,
//             ICSharpTypeSymbol symbol,
//             bool nullable,
//             [MaybeNullWhen(false)] out CSharpValueGenerator result
//         )
//         {
//             if (symbol.IsEnum)
//                 result = EnumGenerator.Get(symbol, renderAsSymbolReference: true, allowNullable: nullable);
//             else if (symbol.Equals(compilation.Int32!))
//                 result = IntegerGenerator.Get(allowNullable: nullable);
//             else if (symbol.Equals(compilation.UInt64!))
//                 result = SnowflakeGenerator.Get(allowNullable: nullable);
//             else if (symbol.Equals(compilation.Boolean))
//                 result = BooleanGenerator.Get(allowNullable: nullable);
//             else result = null;
//
//             return result is not null;
//         }
//     }
//
//     public virtual Result<string> Render(
//         IRendererContext context,
//         CSharpValueGeneratorTarget target,
//         CSharpValueGeneratorOptions options = default,
//         CancellationToken cancellationToken = default
//     ) => target.Value switch
//     {
//         CXValue.Scalar scalar => RenderScalar(context, target, scalar.Token, options, cancellationToken),
//         CXValue.Interpolation interpolation => RenderInterpolation(
//             context,
//             target,
//             interpolation.Token,
//             context.GetInterpolationInfo(interpolation),
//             options,
//             cancellationToken
//         ),
//         CXValue.StringLiteral stringLiteral => RenderStringLiteral(context, target, stringLiteral, options,
//             cancellationToken),
//         CXValue.Multipart multipart => ExtrapolateAndRenderMultipart(context, target, multipart, options,
//             cancellationToken),
//         CXValue.Element element => RenderElementValue(context, target, element, options, cancellationToken),
//         _ => RenderMissingValue(context, target, options, cancellationToken)
//     };
//
//     private Result<string> ExtrapolateAndRenderMultipart(
//         IRendererContext context,
//         CSharpValueGeneratorTarget target,
//         CXValue.Multipart multipart,
//         CSharpValueGeneratorOptions options,
//         CancellationToken cancellationToken = default
//     )
//     {
//         if (multipart is { HasInterpolations: false, Tokens.Count: 1 })
//             return RenderScalar(context, target, multipart.Tokens[0], options, cancellationToken);
//
//         if (multipart.TryGetSingleInterpolation(context, out var info))
//             return RenderInterpolation(context, target, multipart.Tokens[0], info, options, cancellationToken);
//
//         return RenderMultipart(context, target, multipart, options, cancellationToken);
//     }
//
//     protected virtual Result<string> RenderElementValue(
//         IRendererContext context,
//         CSharpValueGeneratorTarget target,
//         CXValue.Element element,
//         CSharpValueGeneratorOptions options,
//         CancellationToken cancellationToken = default
//     ) => target.TextSpan.Report(
//         Diagnostic.ValueVariantCannotBeGenerated(element)
//     );
//
//     protected virtual Result<string> RenderMissingValue(
//         IRendererContext context,
//         CSharpValueGeneratorTarget target,
//         CSharpValueGeneratorOptions options,
//         CancellationToken cancellationToken = default
//     ) => target.TextSpan.Report(
//         Diagnostic.ValueVariantCannotBeGenerated("unknown")
//     );
//
//     protected virtual Result<string> RenderScalar(
//         IRendererContext context,
//         CSharpValueGeneratorTarget target,
//         CXToken token,
//         CSharpValueGeneratorOptions options,
//         CancellationToken cancellationToken = default
//     ) => target.TextSpan.Report(
//         Diagnostic.ValueVariantCannotBeGenerated("scalar")
//     );
//
//     protected virtual Result<string> RenderInterpolation(
//         IRendererContext context,
//         CSharpValueGeneratorTarget target,
//         CXToken token,
//         IInterpolationInfo info,
//         CSharpValueGeneratorOptions options,
//         CancellationToken cancellationToken = default
//     ) => target.TextSpan.Report(
//         Diagnostic.ValueVariantCannotBeGenerated("interpolation")
//     );
//
//     protected virtual Result<string> RenderStringLiteral(
//         IRendererContext context,
//         CSharpValueGeneratorTarget target,
//         CXValue.StringLiteral stringLiteral,
//         CSharpValueGeneratorOptions options,
//         CancellationToken cancellationToken = default
//     ) => ExtrapolateAndRenderMultipart(context, target, stringLiteral, options, cancellationToken);
//
//     protected virtual Result<string> RenderMultipart(
//         IRendererContext context,
//         CSharpValueGeneratorTarget target,
//         CXValue.Multipart multipart,
//         CSharpValueGeneratorOptions options,
//         CancellationToken cancellationToken = default
//     ) => target.TextSpan.Report(
//         Diagnostic.ValueVariantCannotBeGenerated(multipart)
//     );
// }