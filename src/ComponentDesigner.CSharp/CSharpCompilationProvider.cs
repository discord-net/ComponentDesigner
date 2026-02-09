using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using ComponentDesigner.Util;
using Microsoft.CodeAnalysis;

namespace ComponentDesigner.CSharp;

public sealed class CSharpCompilationProvider : ICompilationProvider
{
    private static readonly ConditionalWeakTable<Compilation, CSharpCompilationProvider> _cache = new();
    private static readonly Dictionary<int, WeakReference<ICSharpSymbol>> _symbolsCache = [];

    private readonly Compilation _inner;

    private CSharpCompilationProvider(Compilation inner)
    {
        _inner = inner;
    }

    public static CSharpCompilationProvider Get(Compilation compilation)
    {
        if(!_cache.TryGetValue(compilation, out var provider))
            _cache.Add(compilation, provider = new(compilation));
        
        return provider;
    }

    private T GetSymbol<T>(int key, Func<T> factory)
        where T : ICSharpSymbol
    {
        if (
            _symbolsCache.TryGetValue(key, out var weakRef) &&
            weakRef.TryGetTarget(out var target) && target is T targetSymbol
        )
        {
            return targetSymbol;
        }

        var result = factory();
        _symbolsCache[key] = new WeakReference<ICSharpSymbol>(result);
        return result;
    }

    [return: NotNullIfNotNull(nameof(symbol))]
    public ICSharpTypeSymbol? GetTypeSymbol(ITypeSymbol? symbol)
    {
        if (symbol is null) return null;
        
        return GetSymbol(
            Hash.Combine(
                typeof(ICSharpTypeSymbol),
                symbol.ContainingAssembly.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            ),
            () => new CSharpTypeSymbol(this, symbol)
        );
    }
    
    internal ICSharpFieldSymbol GetFieldSymbol(CSharpTypeSymbol containingType, IFieldSymbol symbol)
        => GetSymbol(
            Hash.Combine(
                typeof(ICSharpFieldSymbol),
                symbol.ContainingAssembly.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            ),
            () => new CSharpFieldSymbol(this, containingType, symbol)
        );


    public ICSharpTypeSymbol? GetTypeFromQualifiedName(string name)
    {
        throw new NotImplementedException();
    }

    public bool HasImplicitConversionBetween(ICSharpTypeSymbol? from, ICSharpTypeSymbol? to,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}