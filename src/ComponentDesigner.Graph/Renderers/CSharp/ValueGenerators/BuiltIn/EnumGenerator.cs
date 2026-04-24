using System.Collections.Immutable;
using ComponentDesigner.Nodes;
using ComponentDesigner.Parser;
using ComponentDesigner.Util;

namespace ComponentDesigner.CSharp;

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

    protected override Result<CSharpRender> RenderInterpolation(
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
                    if (_allowNullable)
                        return new CSharpRender(
                            interpolationInfo.TextSpan,
                            "null",
                            _enumSymbol
                        );

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
            return new CSharpRender(
                interpolationInfo.TextSpan,
                context.GetReferenceToDesignerValue(interpolationInfo, interpolationInfo.Symbol),
                interpolationInfo.Symbol
            );
        }

        return Diagnostic
            .TypeMismatch(
                _enumSymbol,
                interpolationInfo.Symbol!
            )
            .At(interpolationValue);
    }

    protected override Result<CSharpRender> RenderLiteral(
        IRenderContext context,
        ComponentPropertyValue.Literal literalValue,
        string literal,
        CancellationToken cancellationToken = default
    ) => FromText(literal.SourcedAt(literalValue));

    private Result<CSharpRender> FromText(SourcedValue<string> text)
    {
        if (_fields.TryGetValue(text.Value.ToLowerInvariant(), out var field))
            return RenderField(field, text.TextSpan);

        return Diagnostic
            .NotAValidEnumVariant(_enumSymbol.ToString(), text)
            .At(text);
    }

    private CSharpRender RenderField(ICSharpFieldSymbol field, CXTextSpan textSpan)
    {
        if (_renderAsSymbolReference)
            return new(
                textSpan,
                field.ToQualifiedName(),
                field.Type
            );

        if (field.ConstantValue.IsSpecified)
        {
            if (field.Type.BaseType is not null)
                return new(
                    textSpan,
                    $"({field.Type.BaseType.ToQualifiedName()}){field.ConstantValue.Value}",
                    field.Type.BaseType
                );

            return new(
                textSpan,
                field.ConstantValue.Value.ToString()!,
                field.Type
            );
        }

        if (field.Type.BaseType is not null)
            return new(
                textSpan,
                $"({field.Type.BaseType.ToQualifiedName()}){field.ToQualifiedName()}",
                field.Type.BaseType
            );

        return new(
            textSpan,
            field.ToQualifiedName(),
            field.Type
        );
    }
}