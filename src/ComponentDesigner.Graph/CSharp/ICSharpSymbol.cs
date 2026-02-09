namespace ComponentDesigner;

public interface ICSharpSymbol : IEquatable<ICSharpSymbol>
{
    SymbolModifiers Modifiers { get; }
    string Name { get; }
    
    string ToQualifiedName();

    string ToString();

    IReadOnlyList<ICSharpAttributeData> GetAttributes();
}

public interface ICSharpAttributeData : IEquatable<ICSharpAttributeData>
{
    ICSharpTypeSymbol Type { get; }
}