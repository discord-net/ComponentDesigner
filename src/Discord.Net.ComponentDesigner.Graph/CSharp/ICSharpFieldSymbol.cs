namespace Discord.CX;


public interface ICSharpFieldSymbol : ICSharpSymbol, IEquatable<ICSharpFieldSymbol>
{
    
    ICSharpTypeSymbol ContainingType { get; }
    
    ICSharpTypeSymbol Type { get; }
}