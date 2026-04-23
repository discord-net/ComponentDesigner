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
        IRenderContext context,
        ComponentPropertyValue.Interpolation interpolationValue,
        IInterpolationInfo interpolationInfo,
        CancellationToken cancellationToken = default
    )
    {
        if (interpolationInfo.ConstantValue.TryGetOfType(out bool value))
            return value ? "true" : "false";

        if (
            interpolationInfo.ConstantValue.TryGetOfType(out string? strValue) &&
            strValue is not null
        ) return FromText(strValue.SourcedAt(interpolationValue));

        if (
            context.CompilationProvider.HasImplicitConversionBetween(
                interpolationInfo.Symbol,
                context.CompilationProvider.Boolean,
                cancellationToken
            )
            ||
            (
                _allowNullable &&
                interpolationInfo.Symbol.IsNullableTypeOf(context.CompilationProvider.Boolean)
            )
        )
        {
            return context.GetReferenceToDesignerValue(interpolationInfo, interpolationInfo.Symbol);
        }

        return Diagnostic
            .TypeMismatch(
                context.CompilationProvider.Boolean!,
                interpolationInfo.Symbol!
            )
            .At(interpolationValue);
    }

    protected override Result<string> RenderLiteral(
        IRenderContext context,
        ComponentPropertyValue.Literal literalValue,
        string literal,
        CancellationToken cancellationToken = default
    ) => FromText(literal.SourcedAt(literalValue));

    protected override Result<string> RenderNone(
        IRenderContext context,
        ComponentPropertyValue.None noneValue,
        CancellationToken cancellationToken = default
    )
    {
        if (
            noneValue is { IsAttributeNameOnly: true, Property.RequiresValue: false }
        )
        {
            return "true";
        }
        
        return base.RenderNone(context, noneValue, cancellationToken);
    }

    private static Result<string> FromText(
        SourcedValue<string> text
    )
    {
        var lower = text.Value.ToLowerInvariant();

        if (lower is not "true" and not "false")
            return Diagnostic.TypeMismatch("bool", "string").At(text);

        return lower;
    }
}