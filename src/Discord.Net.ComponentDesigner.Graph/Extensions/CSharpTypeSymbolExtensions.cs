namespace Discord.CX;

public static class CSharpTypeSymbolExtensions
{
    extension(ICSharpTypeSymbol symbol)
    {
        public bool IsReferenceType => !symbol.IsValueType;
    }

    extension(ICSharpTypeSymbol? symbol)
    {
        public bool IsNullableTypeOf(ICSharpTypeSymbol inner)
            => symbol is not null && (
                symbol.IsValueType &&
                symbol.Namespace is "System" &&
                symbol.Name is "Nullable" &&
                symbol.TypeArguments.Count is 1 &&
                symbol.TypeArguments[0].Equals(inner)
            );

        public bool CanNullPatternMatch
            => symbol is not null && (
                !symbol.IsValueType ||
                symbol is { IsValueType: true, Namespace: "System", Name: "Nullable", TypeArguments.Count: 1 }
            );
    }
}