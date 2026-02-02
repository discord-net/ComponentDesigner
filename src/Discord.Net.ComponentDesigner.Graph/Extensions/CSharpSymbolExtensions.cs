namespace Discord.CX;

public static class CSharpSymbolExtensions
{
    extension(ICSharpSymbol symbol)
    {
        public bool IsPublic => (symbol.Modifiers & SymbolModifiers.Public) != 0;
        public bool IsInternal => (symbol.Modifiers & SymbolModifiers.Internal) != 0;
        public bool IsPrivate => (symbol.Modifiers & SymbolModifiers.Private) != 0;
        public bool IsStatic => (symbol.Modifiers & SymbolModifiers.Static) != 0;
        public bool IsReadOnly => (symbol.Modifiers & SymbolModifiers.ReadOnly) != 0;
    }
}