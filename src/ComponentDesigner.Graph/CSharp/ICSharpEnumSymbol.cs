namespace ComponentDesigner;

public interface ICSharpEnumSymbol : ICSharpTypeSymbol
{
    IReadOnlyList<ICSharpFieldSymbol> EnumMembers { get; }
}