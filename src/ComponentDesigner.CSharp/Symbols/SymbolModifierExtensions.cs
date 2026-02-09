using Microsoft.CodeAnalysis;

namespace ComponentDesigner.CSharp;

public static class SymbolModifierExtensions
{
    extension(SymbolModifiers)
    {
        public static SymbolModifiers From(ISymbol symbol)
        {
            var modifiers = SymbolModifiers.None;

            switch (symbol.DeclaredAccessibility)
            {
                case Accessibility.Private:
                case Accessibility.ProtectedAndInternal:
                case Accessibility.Protected:
                case Accessibility.ProtectedOrInternal:
                    modifiers |= SymbolModifiers.Private;
                    break;
                case Accessibility.Internal:
                    modifiers |= SymbolModifiers.Internal;
                    break;
                case Accessibility.Public:
                    modifiers |= SymbolModifiers.Public;
                    break;
            }

            if (symbol.IsStatic) modifiers |= SymbolModifiers.Static;

            if (
                symbol is IFieldSymbol { IsReadOnly: true }
                or IPropertySymbol
                {
                    SetMethod: null or
                    { DeclaredAccessibility: not Accessibility.Public and not Accessibility.Internal }
                }
            ) modifiers |= SymbolModifiers.ReadOnly;


            return modifiers;
        }
    }
}