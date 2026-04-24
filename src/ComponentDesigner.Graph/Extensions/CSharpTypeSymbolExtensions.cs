using System.Diagnostics.CodeAnalysis;

namespace ComponentDesigner;

public static class CSharpTypeSymbolExtensions
{
    extension(ICSharpTypeSymbol symbol)
    {
        public bool IsEnum => symbol.TypeKind is TypeKind.Enum;
        
        public IReadOnlyList<ICSharpTypeSymbol> AllInterfaces
        {
            get
            {
                if (symbol.Interfaces.Count is 0) return [];
                
                var result = new List<ICSharpTypeSymbol>();
                var seen = new HashSet<ICSharpTypeSymbol>();
                var queue = new Queue<ICSharpTypeSymbol>(symbol.Interfaces);

                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    
                    if(!seen.Add(current)) continue;
                    
                    result.Add(current);

                    foreach (var next in current.Interfaces)
                    {
                        if(!seen.Add(next)) continue;
                        queue.Enqueue(next);
                    }
                }

                return result;
            }
        }
        
        public bool IsReferenceType => !symbol.IsValueType;
    }

    extension(ICSharpTypeSymbol? symbol)
    {

        public bool Equals(TypeSymbolFactory<CXTextSpan> factory, CancellationToken cancellationToken = default)
        {
            var target = factory(default, cancellationToken).GetValueOrDefault();

            return target is not null && symbol is not null && symbol.Equals(target);
        }
        
        public bool IsNullableTypeOf(ICSharpTypeSymbol? inner)
            => symbol is not null && (
                symbol.IsValueType &&
                symbol.Namespace is "System" &&
                symbol.Name is "Nullable" &&
                symbol.TypeArguments.Count is 1 &&
                symbol.TypeArguments[0].Equals(inner)
            );

        private bool IsNullableWrapperType
            => symbol is { IsValueType: true, Namespace: "System", Name: "Nullable", TypeArguments.Count: 1 };
        
        public bool TryUnwrapNullableValueType([MaybeNullWhen(false)] out ICSharpTypeSymbol inner)
        {
            if (symbol.IsNullableWrapperType)
            {
                inner = symbol!.TypeArguments[0];
                return true;
            }

            inner = null;
            return false;
        }

        public bool CanNullPatternMatch
            => symbol is not null && (
                !symbol.IsValueType ||
                symbol is { IsValueType: true, Namespace: "System", Name: "Nullable", TypeArguments.Count: 1 }
            );

        public bool TryGetEnumerableType([MaybeNullWhen(false)] out ICSharpTypeSymbol inner)
        {
            inner = null;

            if (symbol is null) return false;
            
            if (IsEnumerableType(symbol))
            {
                inner = symbol.TypeArguments[0];
                return true;
            }

            inner = symbol
                .AllInterfaces
                .FirstOrDefault(IsEnumerableType)?
                .TypeArguments
                .FirstOrDefault();

            return inner is not null;

            static bool IsEnumerableType(ICSharpTypeSymbol symbol)
                => symbol is {IsBoundGeneric: true, TypeArguments.Count: 1} && 
                   symbol.ConstructedFrom?.ToQualifiedName() == "global::System.Collections.Generic.IEnumerable<T>";
        }
    }
}