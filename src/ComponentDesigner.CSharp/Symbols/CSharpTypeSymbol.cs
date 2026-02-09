using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace ComponentDesigner.CSharp;

public sealed class CSharpTypeSymbol : ICSharpTypeSymbol, IEquatable<CSharpTypeSymbol>
{
    public string Namespace => _inner.ContainingNamespace.ToDisplayString();

    public ICSharpTypeSymbol? BaseType
        => _inner.BaseType is null ? null : _provider.GetTypeSymbol(_inner.BaseType);

    [field: MaybeNull]
    public IReadOnlyList<ICSharpTypeSymbol> Interfaces
        => field ??= [.._inner.Interfaces.Select(x => _provider.GetTypeSymbol(x))];

    [field: MaybeNull]
    public IReadOnlyList<ICSharpTypeSymbol> TypeArguments
        => field ??=
            _inner is INamedTypeSymbol named
                ? [..named.TypeArguments.Select(x => _provider.GetTypeSymbol(x))]
                : [];

    public bool IsGeneric => _inner is INamedTypeSymbol { IsGenericType: true };

    public bool IsBoundGeneric => IsGeneric && _inner is INamedTypeSymbol { IsUnboundGenericType: false };

    public bool IsValueType => _inner.IsValueType;

    [field: MaybeNull]
    public IReadOnlyList<ICSharpFieldSymbol> Fields
        => field ??=
        [
            .._inner
                .GetMembers()
                .OfType<IFieldSymbol>()
                .Select(x => _provider.GetFieldSymbol(this, x))
        ];

    public ICSharpTypeSymbol? ConstructedFrom => _inner is INamedTypeSymbol named
        ? _provider.GetTypeSymbol(named.ConstructedFrom)
        : null;

    public SymbolModifiers Modifiers { get; }

    public string Name => _inner.Name;

    private readonly ITypeSymbol _inner;
    private readonly CSharpCompilationProvider _provider;

    public CSharpTypeSymbol(CSharpCompilationProvider provider, ITypeSymbol inner)
    {
        _provider = provider;
        _inner = inner;

        Modifiers = SymbolModifiers.From(inner);
    }


    public string ToQualifiedName() => _inner.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    public bool Equals(CSharpTypeSymbol? symbol)
        => symbol is not null &&
           _inner.Equals(symbol._inner, SymbolEqualityComparer.Default);


    public bool Equals(ICSharpTypeSymbol? obj)
        => obj is CSharpTypeSymbol other && Equals(other);

    public bool Equals(ICSharpSymbol? obj)
        => obj is CSharpTypeSymbol other && Equals(other);

    public override bool Equals(object? obj)
        => obj is CSharpTypeSymbol other && Equals(other);
}