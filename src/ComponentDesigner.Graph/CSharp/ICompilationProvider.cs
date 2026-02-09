namespace ComponentDesigner;

public interface ICompilationProvider
{
    ICSharpTypeSymbol? GetTypeFromQualifiedName(string name, CancellationToken cancellationToken = default);

    bool HasImplicitConversionBetween(
        ICSharpTypeSymbol? from,
        ICSharpTypeSymbol? to,
        CancellationToken cancellationToken = default
    );

    IReadOnlyList<ICSharpSymbol> LookupSymbols(
        LocationInfo location, string name, ICSharpTypeSymbol? container = null,
        CancellationToken cancellationToken = default
    );
}