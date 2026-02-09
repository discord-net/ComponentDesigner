namespace ComponentDesigner;


public interface ICSharpFieldSymbol : ICSharpSymbol, IEquatable<ICSharpFieldSymbol>
{
    
    ICSharpTypeSymbol ContainingType { get; }
    
    ICSharpTypeSymbol Type { get; }
    
    Optional<object> ConstantValue { get; }
}