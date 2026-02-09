namespace ComponentDesigner;

public interface ICSharpTypeSymbol : ICSharpSymbol, IEquatable<ICSharpTypeSymbol>
{
    string Namespace { get; }
    
    ICSharpTypeSymbol? BaseType { get; }
    
    IReadOnlyList<ICSharpTypeSymbol> Interfaces { get; }
    
    IReadOnlyList<ICSharpTypeSymbol> TypeArguments { get; }
    
    ICSharpTypeSymbol? ConstructedFrom { get; }
    
    bool IsGeneric { get; }
    
    bool IsBoundGeneric { get; }
    
    bool IsValueType { get; }

    IReadOnlyList<ICSharpFieldSymbol> Fields { get; }
}