using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Discord.CX;

public static class TypeUtils
{
    public static bool IsNumericType(this Type type)
        => type == typeof(byte) ||
           type == typeof(sbyte) ||
           type == typeof(ushort) ||
           type == typeof(short) ||
           type == typeof(uint) ||
           type == typeof(int) ||
           type == typeof(ulong) ||
           type == typeof(long);
    
    public static bool IsInTypeTree(this ITypeSymbol symbol, ITypeSymbol? other)
    {
        if (other is null) return false;
        
        if (symbol.TypeKind is TypeKind.Class)
        {
            var current = symbol;

            while (current is not null)
            {
                if (other.Equals(current, SymbolEqualityComparer.Default)) return true;

                current = current.BaseType;
            }

            return false;
        }

        return other.Equals(symbol, SymbolEqualityComparer.Default) ||
               other.AllInterfaces.Contains(symbol, SymbolEqualityComparer.Default);
    }
    
    public static bool TryGetEnumerableType(this ITypeSymbol? symbol, out ITypeSymbol inner)
    {
        if (symbol is not INamedTypeSymbol named)
        {
            inner = null!;
            return false;
        }
        
        if (IsEnumerableType(named) && named.TypeArguments.Length is 1)
        {
            inner = named.TypeArguments[0];
            return true;
        }

        inner = named
            .AllInterfaces
            .FirstOrDefault(IsEnumerableType)
            ?.TypeArguments
            .FirstOrDefault()!;

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        return inner is not null;
    }

    private static bool IsEnumerableType(this INamedTypeSymbol symbol)
        => symbol.IsGenericType && symbol.ConstructedFrom.SpecialType is SpecialType.System_Collections_Generic_IEnumerable_T;
}