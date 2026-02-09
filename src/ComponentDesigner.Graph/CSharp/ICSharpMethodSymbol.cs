namespace ComponentDesigner;

public interface ICSharpParameterSymbol : ICSharpSymbol, IEquatable<ICSharpParameterSymbol>
{
    ICSharpTypeSymbol Type { get; }
    bool HasDefaultValue { get; }
}

public interface ICSharpMethodSymbol : ICSharpSymbol, IEquatable<ICSharpMethodSymbol>
{
    IReadOnlyList<ICSharpParameterSymbol> Parameters { get; }
    ICSharpTypeSymbol ReturnType { get; }
    ICSharpTypeSymbol ContainingType { get; }
}