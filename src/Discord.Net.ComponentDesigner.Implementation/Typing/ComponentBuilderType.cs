using ComponentDesigner;

namespace Discord;

public readonly record struct ComponentBuilderType(
    ComponentBuilderKind Kind,
    bool IsCollection
)
{
    public static bool TryGetFromSymbol(
        ICSharpTypeSymbol? symbol,
        ICompilationProvider compilationProvider,
        CancellationToken cancellationToken,
        out ComponentBuilderType type
    )
    {
        if (symbol is null)
        {
            type = default;
            return false;
        }

        var kind = ComponentBuilderKind.None;
        var isCollection = false;

        var current = symbol;

        if (
            !current.Equals(compilationProvider.String!) &&
            current.TryGetEnumerableType(out var enumerableInnerType)
        )
        {
            current = enumerableInnerType;
            isCollection = true;
        }

        if (AssignableFrom(compilationProvider.IMessageComponentBuilder))
        {
            kind = ComponentBuilderKind.IMessageComponentBuilder;
        }
        else if (AssignableFrom(compilationProvider.IMessageComponent))
        {
            kind = ComponentBuilderKind.IMessageComponent;
        }
        else if (Is(compilationProvider.MessageComponent))
        {
            kind = ComponentBuilderKind.MessageComponent;
        }
        else if (Is(compilationProvider.ComponentBuilderV2))
        {
            kind = ComponentBuilderKind.ComponentBuilderV2;
        }
        else if (Is(compilationProvider.ModalComponent))
        {
            kind = ComponentBuilderKind.ModalComponent;
        }
        else if (Is(compilationProvider.ModalBuilder))
        {
            kind = ComponentBuilderKind.ModalBuilder;
        } 
        else if (Is(compilationProvider.CXComponent))
        {
            kind = ComponentBuilderKind.CXComponent;
        } 
        else if (Is(compilationProvider.CXMessageComponent))
        {
            kind = ComponentBuilderKind.CXMessageComponent;
        }
        else if (Is(compilationProvider.CXModalComponent))
        {
            kind = ComponentBuilderKind.CXModalComponent;
        }
        else if (Is(compilationProvider.SelectMenuOptionBuilder))
        {
            kind = ComponentBuilderKind.SelectMenuOptionBuilder;
        }

        if (kind is not ComponentBuilderKind.None)
        {
            type = new(kind, isCollection);
            return true;
        }

        type = default;
        return false;

        bool Is(Func<CXTextSpan, CancellationToken, Result<ICSharpTypeSymbol>> func)
            => current.Equals(func(default, cancellationToken).GetValueOrDefault()!);

        bool AssignableFrom(Func<CXTextSpan, CancellationToken, Result<ICSharpTypeSymbol>> func)
            => compilationProvider.HasImplicitConversionBetween(
                current,
                func(default, cancellationToken).GetValueOrDefault(),
                cancellationToken
            );
    }
}