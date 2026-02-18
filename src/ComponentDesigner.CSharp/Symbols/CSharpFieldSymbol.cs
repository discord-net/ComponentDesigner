using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace ComponentDesigner.CSharp;

public sealed class CSharpFieldSymbol : ICSharpFieldSymbol, IEquatable<CSharpFieldSymbol>
{
    public SymbolModifiers Modifiers { get; }

    public string Name => _inner.Name;
    
    public ICSharpTypeSymbol ContainingType { get; }

    [field: MaybeNull]
    public ICSharpTypeSymbol Type
        => field ??= _provider.GetTypeSymbol(_inner.Type);

    public Optional<object> ConstantValue => _inner.HasConstantValue ? new(_inner.ConstantValue) : default;

    private readonly CSharpCompilationProvider _provider;
    private readonly IFieldSymbol _inner;

    public CSharpFieldSymbol(CSharpCompilationProvider provider, CSharpTypeSymbol containingType, IFieldSymbol inner)
    {
        ContainingType = containingType;
        _provider = provider;
        _inner = inner;

        Modifiers = SymbolModifiers.From(inner);
    }

    public string ToQualifiedName()
        => $"{ContainingType.ToQualifiedName()}.{Name}";

    public override string ToString()
        => _inner.ToDisplayString();
    
    public IReadOnlyList<ICSharpAttributeData> GetAttributes()
        => [.._inner.GetAttributes().Select(x => new CSharpAttributeData(_provider, x))];

    public bool Equals(CSharpFieldSymbol? other)
        => other is not null && _inner.Equals(other._inner, SymbolEqualityComparer.Default);

    public bool Equals(ICSharpFieldSymbol? obj)
        => obj is CSharpFieldSymbol other && Equals(other);
    
    public bool Equals(ICSharpSymbol? obj)
        => obj is CSharpFieldSymbol other && Equals(other);

    public override bool Equals(object? obj)
        => obj is CSharpFieldSymbol other && Equals(other);
}