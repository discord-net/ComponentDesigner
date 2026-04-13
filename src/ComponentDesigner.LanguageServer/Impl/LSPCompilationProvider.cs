using ComponentDesigner;

namespace Discord.ComponentDesigner.LanguageServer;

public sealed class LSPCompilationProvider : ICompilationProvider
{
    public static readonly LSPCompilationProvider Instance = new();

    public ICSharpTypeSymbol? GetTypeFromQualifiedName(string name, CancellationToken cancellationToken = default)
    {
        return null;
    }

    public bool HasImplicitConversionBetween(
        ICSharpTypeSymbol? from,
        ICSharpTypeSymbol? to,
        CancellationToken cancellationToken = default
    )
    {
        return false;
    }

    public IReadOnlyList<ICSharpSymbol> LookupSymbols(
        ICXModel cxModel,
        string name,
        ICSharpTypeSymbol? container = null,
        CancellationToken cancellationToken = default
    )
    {
        return [];
    }
}