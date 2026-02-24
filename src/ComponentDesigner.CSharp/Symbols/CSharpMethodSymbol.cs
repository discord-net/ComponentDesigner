using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace ComponentDesigner.CSharp;

public sealed class CSharpMethodSymbol : ICSharpMethodSymbol
{
    public SymbolModifiers Modifiers { get; }

    public string Name => Inner.Name;

    [field: MaybeNull]
    public IReadOnlyList<ICSharpParameterSymbol> Parameters
        => field ??= [..Inner.Parameters.Select(ICSharpParameterSymbol (x) => new CSharpParameterSymbol(_provider, x))];

    [field: MaybeNull]
    public ICSharpTypeSymbol ReturnType
        => field ??= _provider.GetTypeSymbol(Inner.ReturnType);

    [field: MaybeNull]
    public ICSharpTypeSymbol ContainingType
        => field ??= _provider.GetTypeSymbol(Inner.ContainingType);


    public IMethodSymbol Inner { get; }

    private readonly CSharpCompilationProvider _provider;


    public CSharpMethodSymbol(CSharpCompilationProvider provider, IMethodSymbol inner)
    {
        _provider = provider;
        Inner = inner;

        Modifiers = SymbolModifiers.From(inner);
    }

    public string ToQualifiedName()
        => $"{ContainingType.ToQualifiedName()}.{Name}";

    public IReadOnlyList<ICSharpAttributeData> GetAttributes()
        => [..Inner.GetAttributes().Select(x => new CSharpAttributeData(_provider, x))];

    public bool Equals(ICSharpMethodSymbol? obj)
        => obj is CSharpMethodSymbol other && Inner.Equals(other.Inner, SymbolEqualityComparer.Default);

    public bool Equals(ICSharpSymbol? obj)
        => obj is ICSharpMethodSymbol other && Equals(other);
}

public sealed class CSharpParameterSymbol : ICSharpParameterSymbol
{
    public SymbolModifiers Modifiers { get; }

    public string Name => Inner.Name;

    [field: MaybeNull] public ICSharpTypeSymbol Type => field ??= _provider.GetTypeSymbol(Inner.Type);

    public bool HasDefaultValue => Inner.HasExplicitDefaultValue;

    public IParameterSymbol Inner { get; }

    private readonly CSharpCompilationProvider _provider;

    public CSharpParameterSymbol(CSharpCompilationProvider provider, IParameterSymbol inner)
    {
        _provider = provider;
        Inner = inner;

        Modifiers = SymbolModifiers.From(inner);
    }

    public string ToQualifiedName()
        => $"{Type.ToQualifiedName()} {Name}";

    public IReadOnlyList<ICSharpAttributeData> GetAttributes()
        => [..Inner.GetAttributes().Select(x => new CSharpAttributeData(_provider, x))];

    public bool Equals(ICSharpParameterSymbol? obj)
        => obj is CSharpParameterSymbol { } other && Inner.Equals(other.Inner, SymbolEqualityComparer.Default);

    public bool Equals(ICSharpSymbol? other)
        => other is ICSharpParameterSymbol parameter && Equals(parameter);
}