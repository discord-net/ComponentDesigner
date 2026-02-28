using System.Diagnostics.CodeAnalysis;
using ComponentDesigner.Nodes;
using ComponentDesigner.Parser;

namespace ComponentDesigner;

public abstract class CSharpValueGenerator
{
    public static CSharpValueGenerator Boolean => BooleanGenerator.Get(allowNullable: false);
    public static CSharpValueGenerator NullableBoolean => BooleanGenerator.Get(allowNullable: true);
    public static CSharpValueGenerator Integer => IntegerGenerator.Get(allowNullable: false);
    public static CSharpValueGenerator NullableInteger => IntegerGenerator.Get(allowNullable: true);
    public static CSharpValueGenerator Snowflake => SnowflakeGenerator.Get(allowNullable: false);
    public static CSharpValueGenerator NullableSnowflake => SnowflakeGenerator.Get(allowNullable: true);
    public static CSharpValueGenerator String => StringGenerator.Get(StringNullMode.DisallowNull);
    public static CSharpValueGenerator NullableString => StringGenerator.Get(StringNullMode.AllowNull);
    
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
            else if (symbol.Equals(compilation.Int32!))
                result = IntegerGenerator.Get(allowNullable: nullable);
            else if (symbol.Equals(compilation.UInt64!))
                result = SnowflakeGenerator.Get(allowNullable: nullable);
            else if (symbol.Equals(compilation.Boolean))
                result = BooleanGenerator.Get(allowNullable: nullable);
            else result = null;

            return result is not null;
        }
    }

    public virtual Result<string> Render(
        IRendererContext context,
        CSharpValueGeneratorTarget target,
        CSharpValueGeneratorOptions options = default,
        CancellationToken cancellationToken = default
    ) => target.Value switch
    {
        CXValue.Scalar scalar => RenderScalar(context, target, scalar.Token, options, cancellationToken),
        CXValue.Interpolation interpolation => RenderInterpolation(
            context,
            target,
            interpolation.Token,
            context.GetInterpolationInfo(interpolation),
            options,
            cancellationToken
        ),
        CXValue.StringLiteral stringLiteral => RenderStringLiteral(context, target, stringLiteral, options,
            cancellationToken),
        CXValue.Multipart multipart => ExtrapolateAndRenderMultipart(context, target, multipart, options,
            cancellationToken),
        CXValue.Element element => RenderElementValue(context, target, element, options, cancellationToken),
        _ => RenderMissingValue(context, target, options, cancellationToken)
    };

    private Result<string> ExtrapolateAndRenderMultipart(
        IRendererContext context,
        CSharpValueGeneratorTarget target,
        CXValue.Multipart multipart,
        CSharpValueGeneratorOptions options,
        CancellationToken cancellationToken = default
    )
    {
        if (multipart is { HasInterpolations: false, Tokens.Count: 1 })
            return RenderScalar(context, target, multipart.Tokens[0], options, cancellationToken);

        if (multipart.TryGetSingleInterpolation(context, out var info))
            return RenderInterpolation(context, target, multipart.Tokens[0], info, options, cancellationToken);

        return RenderMultipart(context, target, multipart, options, cancellationToken);
    }

    protected virtual Result<string> RenderElementValue(
        IRendererContext context,
        CSharpValueGeneratorTarget target,
        CXValue.Element element,
        CSharpValueGeneratorOptions options,
        CancellationToken cancellationToken = default
    ) => target.TextSpan.Report(
        Diagnostic.ValueVariantCannotBeGenerated(element)
    );

    protected virtual Result<string> RenderMissingValue(
        IRendererContext context,
        CSharpValueGeneratorTarget target,
        CSharpValueGeneratorOptions options,
        CancellationToken cancellationToken = default
    ) => target.TextSpan.Report(
        Diagnostic.ValueVariantCannotBeGenerated("unknown")
    );

    protected virtual Result<string> RenderScalar(
        IRendererContext context,
        CSharpValueGeneratorTarget target,
        CXToken token,
        CSharpValueGeneratorOptions options,
        CancellationToken cancellationToken = default
    ) => target.TextSpan.Report(
        Diagnostic.ValueVariantCannotBeGenerated("scalar")
    );

    protected virtual Result<string> RenderInterpolation(
        IRendererContext context,
        CSharpValueGeneratorTarget target,
        CXToken token,
        IInterpolationInfo info,
        CSharpValueGeneratorOptions options,
        CancellationToken cancellationToken = default
    ) => target.TextSpan.Report(
        Diagnostic.ValueVariantCannotBeGenerated("interpolation")
    );

    protected virtual Result<string> RenderStringLiteral(
        IRendererContext context,
        CSharpValueGeneratorTarget target,
        CXValue.StringLiteral stringLiteral,
        CSharpValueGeneratorOptions options,
        CancellationToken cancellationToken = default
    ) => ExtrapolateAndRenderMultipart(context, target, stringLiteral, options, cancellationToken);

    protected virtual Result<string> RenderMultipart(
        IRendererContext context,
        CSharpValueGeneratorTarget target,
        CXValue.Multipart multipart,
        CSharpValueGeneratorOptions options,
        CancellationToken cancellationToken = default
    ) => target.TextSpan.Report(
        Diagnostic.ValueVariantCannotBeGenerated(multipart)
    );
}