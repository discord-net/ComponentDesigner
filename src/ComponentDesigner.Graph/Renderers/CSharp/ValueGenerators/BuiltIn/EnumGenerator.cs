using ComponentDesigner.Parser;
using ComponentDesigner.Util;

namespace ComponentDesigner;

public sealed class EnumGenerator : CSharpValueGenerator
{
    private readonly ICSharpEnumSymbol _enumSymbol;
    private readonly bool _renderAsSymbolReference;
    private readonly bool _allowNullable;

    private EnumGenerator(
        ICSharpEnumSymbol enumSymbol,
        bool renderAsSymbolReference,
        bool allowNullable
    )
    {
        _enumSymbol = enumSymbol;
        _renderAsSymbolReference = renderAsSymbolReference;
        _allowNullable = allowNullable;
    }

    public static EnumGenerator Get(
        ICSharpEnumSymbol symbol,
        bool renderAsSymbolReference,
        bool allowNullable
    ) => WeakMemoize.Of(
        symbol,
        renderAsSymbolReference,
        allowNullable,
        static (a, b, c) => new EnumGenerator(a, b, c)
    );

    protected override Result<string> RenderScalar(
        IRendererContext context,
        CSharpValueGeneratorTarget target,
        CXToken token,
        CSharpValueGeneratorOptions options,
        CancellationToken cancellationToken = default
    ) => FromText(context, token.Span, token.Value, cancellationToken);

    protected override Result<string> RenderInterpolation(
        IRendererContext context,
        CSharpValueGeneratorTarget target,
        CXToken token,
        IInterpolationInfo info,
        CSharpValueGeneratorOptions options,
        CancellationToken cancellationToken = default
    )
    {
        if (info.ConstantValue.IsSpecified)
        {
            if (info.ConstantValue.Value is null)
            {
                if (_allowNullable) return "null";

                return token.Report(
                    Diagnostic.NullValueNotAllowed
                );
            }

            if (info.ConstantValue.Value is string str)
                return FromText(context, token.Span, str, cancellationToken);

            // check for conversion of numbers
            if (
                _enumSymbol.BaseType is not null &&
                context.CompilationProvider.HasImplicitConversionBetween(
                    info.Symbol,
                    _enumSymbol.BaseType,
                    cancellationToken
                )
            )
            {
                return $"({_enumSymbol.ToQualifiedName()}){info.ConstantValue.Value}";
            }
        }

        if (
            context.CompilationProvider.HasImplicitConversionBetween(
                info.Symbol,
                _enumSymbol,
                cancellationToken
            )
            ||
            (
                _allowNullable &&
                info.Symbol.IsNullableTypeOf(_enumSymbol)
            )
        )
        {
            var designer = context.GetReferenceToDesignerValue(info, info.Symbol);

            if (_renderAsSymbolReference || _enumSymbol.BaseType is null) return designer;

            return $"({_enumSymbol.BaseType.ToQualifiedName()}){designer}";
        }

        return token.Report(
            Diagnostic.TypeMismatch(
                _enumSymbol,
                info.Symbol!
            )
        );
    }

    protected override Result<string> RenderMultipart(
        IRendererContext context,
        CSharpValueGeneratorTarget target,
        CXValue.Multipart multipart,
        CSharpValueGeneratorOptions options,
        CancellationToken cancellationToken = default
    ) => multipart.Report(
        Diagnostic.NotAValidEnumVariant(_enumSymbol.ToString(), "<multipart>")
    );

    private Result<string> FromText(
        IRendererContext context,
        CXTextSpan textSpan,
        string text,
        CancellationToken cancellationToken
    )
    {
        var field = _enumSymbol
            .EnumMembers
            .FirstOrDefault(x => x
                .Name.Equals(text, StringComparison.InvariantCultureIgnoreCase)
            );

        if (field is not null) return RenderField(field);

        return textSpan.Report(
            Diagnostic.NotAValidEnumVariant(_enumSymbol.ToString(), text)
        );
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