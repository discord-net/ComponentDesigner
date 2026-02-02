namespace Discord.CX;

public interface ICSharpSymbol : IEquatable<ICSharpSymbol>
{
    SymbolModifiers Modifiers { get; }
    string Name { get; }
    
    string ToQualifiedName();

    string ToString();
}