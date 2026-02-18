using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace ComponentDesigner.CSharp;

public sealed class CSharpTypeSymbol : ICSharpTypeSymbol, IEquatable<CSharpTypeSymbol>
{
    public TypeKind TypeKind => (TypeKind)InnerSymbol.TypeKind;

    public string Namespace => InnerSymbol.ContainingNamespace.ToDisplayString();

    public ICSharpTypeSymbol? BaseType
        => InnerSymbol.BaseType is null ? null : _provider.GetTypeSymbol(InnerSymbol.BaseType);

    [field: MaybeNull]
    public IReadOnlyList<ICSharpTypeSymbol> Interfaces
        => field ??= [..InnerSymbol.Interfaces.Select(x => _provider.GetTypeSymbol(x))];

    [field: MaybeNull]
    public IReadOnlyList<ICSharpTypeSymbol> TypeArguments
        => field ??=
            InnerSymbol is INamedTypeSymbol named
                ? [..named.TypeArguments.Select(x => _provider.GetTypeSymbol(x))]
                : [];

    public bool IsGeneric => InnerSymbol is INamedTypeSymbol { IsGenericType: true };

    public bool IsBoundGeneric => IsGeneric && InnerSymbol is INamedTypeSymbol { IsUnboundGenericType: false };

    public bool IsValueType => InnerSymbol.IsValueType;

    [field: MaybeNull]
    public IReadOnlyList<ICSharpFieldSymbol> Fields
        => field ??=
        [
            ..InnerSymbol
                .GetMembers()
                .OfType<IFieldSymbol>()
                .Select(x => _provider.GetFieldSymbol(this, x))
        ];

    public ICSharpTypeSymbol? ConstructedFrom => InnerSymbol is INamedTypeSymbol named
        ? _provider.GetTypeSymbol(named.ConstructedFrom)
        : null;

    public SymbolModifiers Modifiers { get; }

    public string Name => InnerSymbol.Name;

    public ITypeSymbol InnerSymbol { get; }
    private readonly CSharpCompilationProvider _provider;

    public CSharpTypeSymbol(CSharpCompilationProvider provider, ITypeSymbol inner)
    {
        _provider = provider;
        InnerSymbol = inner;

        Modifiers = SymbolModifiers.From(inner);
    }


    public string ToQualifiedName() => InnerSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    public override string ToString()
        => InnerSymbol.ToDisplayString();

    public IReadOnlyList<ICSharpAttributeData> GetAttributes()
        => [..InnerSymbol.GetAttributes().Select(x => new CSharpAttributeData(_provider, x))];

    public bool Equals(CSharpTypeSymbol? symbol)
        => symbol is not null &&
           InnerSymbol.Equals(symbol.InnerSymbol, SymbolEqualityComparer.Default);
    
    public bool Equals(ICSharpTypeSymbol? obj)
        => obj is CSharpTypeSymbol other && Equals(other);

    public bool Equals(ICSharpSymbol? obj)
        => obj is CSharpTypeSymbol other && Equals(other);

    public override bool Equals(object? obj)
        => obj is CSharpTypeSymbol other && Equals(other);
}