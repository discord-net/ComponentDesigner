namespace Discord.CX;

public interface ICompilationProvider
{
    ICSharpTypeSymbol? GetTypeFromQualifiedName(string name);
    
    bool HasImplicitConversionBetween(
        ICSharpTypeSymbol? from,
        ICSharpTypeSymbol? to,
        CancellationToken token = default
    );
}