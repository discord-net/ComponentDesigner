using System.Collections.Immutable;
using ComponentDesigner.Nodes;
using ComponentDesigner.Parser;
using ComponentDesigner.Util;

namespace ComponentDesigner;

public sealed class EnumGenerator : CSharpValueGenerator
{
    private readonly ICSharpTypeSymbol _enumSymbol;
    private readonly bool _renderAsSymbolReference;
    private readonly bool _allowNullable;

    private readonly Dictionary<string, ICSharpFieldSymbol> _fields;

    private EnumGenerator(
        ICSharpTypeSymbol enumSymbol,
        bool renderAsSymbolReference,
        bool allowNullable
    )
    {
        _enumSymbol = enumSymbol;
        _renderAsSymbolReference = renderAsSymbolReference;
        _allowNullable = allowNullable;
        _fields = enumSymbol
            .Fields
            .Where(x =>
                x.Type.Equals(enumSymbol) &&
                x is { IsStatic: true, IsReadOnly: true, IsPublic: true }
            )
            .ToDictionary(x => x.Name.ToLowerInvariant());
    }

    public static EnumGenerator Get(
        ICSharpTypeSymbol symbol,
        bool renderAsSymbolReference,
        bool allowNullable
    ) => WeakMemoize.Of(
        symbol,
        renderAsSymbolReference,
        allowNullable,
        static (a, b, c) => new EnumGenerator(a, b, c)
    );

    protected override Result<string> RenderInterpolation(
        IRenderContext context,
        ComponentPropertyValue.Interpolation interpolationValue,
        IInterpolationInfo interpolationInfo,
        CancellationToken cancellationToken = default
    )
    {
        if (interpolationInfo.ConstantValue.IsSpecified)
        {
            switch (interpolationInfo.ConstantValue.Value)
            {
                case null:
                    if (_allowNullable) return "null";

                    return Diagnostic
                        .NullValueNotAllowed
                        .At(interpolationValue);

                case string str:
                    return FromText(str.SourcedAt(interpolationValue));

                // TODO: maybe numeric type conversions?
            }
        }

        if (
            context.CompilationProvider.HasImplicitConversionBetween(
                interpolationInfo.Symbol,
                _enumSymbol,
                cancellationToken
            )
            ||
            (
                _allowNullable &&
                interpolationInfo.Symbol.IsNullableTypeOf(_enumSymbol)
            )
        )
        {
            return context.GetReferenceToDesignerValue(interpolationInfo, interpolationInfo.Symbol);
        }

        return Diagnostic
            .TypeMismatch(
                _enumSymbol,
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

    private Result<string> FromText(SourcedValue<string> text)
    {
        if (_fields.TryGetValue(text.Value.ToLowerInvariant(), out var field))
            return RenderField(field);

        return Diagnostic
            .NotAValidEnumVariant(_enumSymbol.ToString(), text)
            .At(text);
    }

    private string RenderField(ICSharpFieldSymbol field)
    {
        if (_renderAsSymbolReference) return field.ToQualifiedName();

        if (field.ConstantValue.IsSpecified)
        {
            if (field.Type.BaseType is not null)
                return $"({field.Type.BaseType.ToQualifiedName()}){field.ConstantValue.Value}";

            return field.ConstantValue.Value.ToString()!;
        }

        if (field.Type.BaseType is not null)
            return $"({field.Type.BaseType.ToQualifiedName()}){field.ToQualifiedName()}";

        return field.ToQualifiedName();
    }
}