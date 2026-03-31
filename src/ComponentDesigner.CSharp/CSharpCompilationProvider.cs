using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using ComponentDesigner.Util;
using Microsoft.CodeAnalysis;

namespace ComponentDesigner.CSharp;

public sealed class CSharpCompilationProvider : ICompilationProvider
{
    private static readonly ConditionalWeakTable<Compilation, CSharpCompilationProvider> _cache = new();
    private readonly Dictionary<int, WeakReference<ICSharpSymbol>> _symbolsCache = [];

    public Compilation Inner { get; }

    private CSharpCompilationProvider(Compilation inner)
    {
        Inner = inner;
    }

    public static CSharpCompilationProvider Get(Compilation compilation)
    {
        if (!_cache.TryGetValue(compilation, out var provider))
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
    public CSharpTypeSymbol? GetTypeSymbol(ITypeSymbol? symbol)
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
    
    [return: NotNullIfNotNull(nameof(symbol))]
    public CSharpMethodSymbol? GetMethodSymbol(IMethodSymbol? symbol)
    {
        if (symbol is null) return null;

        return GetSymbol(
            Hash.Combine(
                typeof(ICSharpMethodSymbol),
                symbol.ContainingAssembly.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            ),
            () => new CSharpMethodSymbol(this, symbol)
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


    public CSharpTypeSymbol? GetTypeFromQualifiedName(string name, CancellationToken cancellationToken = default)
    {
        var symbol = Inner.GetTypeByMetadataName(name);

        if (symbol is not null) return GetTypeSymbol(symbol);


        return null;
    }

    public bool HasImplicitConversionBetween(
        ICSharpTypeSymbol? from,
        ICSharpTypeSymbol? to,
        CancellationToken cancellationToken = default
    ) => Inner.HasImplicitConversion(
        GetTypeFromImplementation(from, cancellationToken),
        GetTypeFromImplementation(to, cancellationToken)
    );

    public IReadOnlyList<ICSharpSymbol> LookupSymbols(
        ICXModel cxModel,
        string name,
        ICSharpTypeSymbol? container = null,
        CancellationToken cancellationToken = default
    )
    {
        var tree = FindSyntaxTree(cxModel.Location);

        if (tree is null) return [];

        return
        [
            ..Inner
                .GetSemanticModel(tree)
                .LookupSymbols(
                    cxModel.Location.TextSpan.Start,
                    GetTypeFromImplementation(container, cancellationToken),
                    name
                )
                .Select(ToCSharpSymbol)
                .Where(x => x is not null)!
        ];
    }

    private ICSharpSymbol? ToCSharpSymbol(ISymbol symbol)
    {
        return symbol switch
        {
            ITypeSymbol typeSymbol => GetTypeSymbol(typeSymbol),
            IFieldSymbol field => GetFieldSymbol(GetTypeSymbol(field.ContainingType), field),
            IMethodSymbol method => GetMethodSymbol(method),
            _ => null
        };
    }

    private SyntaxTree? FindSyntaxTree(LocationInfo locationInfo)
    {
        foreach (var tree in Inner.SyntaxTrees)
        {
            try
            {
                var lineSpan = tree.GetLineSpan(locationInfo.TextSpan.AsRoslynTextSpan);

                if (
                    locationInfo.LineSpan.Start.Line != lineSpan.StartLinePosition.Line ||
                    locationInfo.LineSpan.Start.Character != lineSpan.StartLinePosition.Character ||
                    locationInfo.LineSpan.End.Line != lineSpan.EndLinePosition.Line ||
                    locationInfo.LineSpan.End.Character != lineSpan.EndLinePosition.Character
                ) continue;
            }
            catch
            {
                continue;
            }

            if (tree.FilePath == locationInfo.FilePath) return tree;
        }

        return null;
    }

    private ITypeSymbol? GetTypeFromImplementation(
        ICSharpTypeSymbol? symbol,
        CancellationToken cancellationToken = default
    )
    {
        if (symbol is null) return null;

        if (symbol is CSharpTypeSymbol { InnerSymbol: { } innerSymbol })
        {
            return innerSymbol;
        }

        return GetTypeFromQualifiedName(symbol.ToString(), cancellationToken)?.InnerSymbol;
    }

    ICSharpTypeSymbol? ICompilationProvider.GetTypeFromQualifiedName(string name, CancellationToken cancellationToken)
        => GetTypeFromQualifiedName(name, cancellationToken);
}