namespace ComponentDesigner;

public delegate Result<ICSharpTypeSymbol> TypeSymbolFactory<in TSource>(
    TSource source,
    CancellationToken cancellationToken = default
) where TSource : ISourceLocatable;

public delegate Result<ICSharpTypeSymbol> StaticTypeSymbolFactory<in TSource>(
    ICompilationProvider provider,
    TSource source,
    CancellationToken cancellationToken = default
) where TSource : ISourceLocatable;

public static class CompilationProviderExtension
{
    extension(ICompilationProvider compilation)
    {
        public Result<TSymbol> GetTypeSymbol<TSymbol, TSource>(
            string name,
            TSource source,
            CancellationToken cancellationToken = default
        ) where TSymbol : ICSharpTypeSymbol where TSource : ISourceLocatable
            => compilation.GetTypeSymbol<TSymbol>(name, source.TextSpan, cancellationToken);

        public Result<TSymbol> GetTypeSymbol<TSymbol>(
            string name,
            CXTextSpan reference,
            CancellationToken cancellationToken = default
        ) where TSymbol : ICSharpTypeSymbol
        {
            var symbol = compilation.GetTypeFromQualifiedName(name, cancellationToken);

            if (symbol is not TSymbol expected)
                return Diagnostic.TypeNotFound(name).At(reference);

            return new(expected);
        }

        public Result<ICSharpTypeSymbol> GetTypeSymbol(
            string name,
            CXTextSpan reference,
            CancellationToken cancellationToken = default
        ) => compilation.GetTypeSymbol<ICSharpTypeSymbol>(name, reference, cancellationToken);

        public Result<ICSharpTypeSymbol> GetTypeSymbol<TSource>(
            string name,
            TSource source,
            CancellationToken cancellationToken = default
        ) where TSource : ISourceLocatable
            => compilation.GetTypeSymbol<ICSharpTypeSymbol>(name, source.TextSpan, cancellationToken);

        public Result<ICSharpTypeSymbol> Boolean<T>(T source, CancellationToken cancellationToken = default)
            where T : ISourceLocatable
            => compilation.GetTypeSymbol("System.Boolean", source, cancellationToken);

        public Result<ICSharpTypeSymbol> String<T>(T source, CancellationToken cancellationToken = default)
            where T : ISourceLocatable
            => compilation.GetTypeSymbol("System.String", source, cancellationToken);

        public Result<ICSharpTypeSymbol> UInt8<T>(T source, CancellationToken cancellationToken = default)
            where T : ISourceLocatable
            => compilation.GetTypeSymbol("System.UInt8", source, cancellationToken);

        public Result<ICSharpTypeSymbol> UInt16<T>(T source, CancellationToken cancellationToken = default)
            where T : ISourceLocatable
            => compilation.GetTypeSymbol("System.UInt16", source, cancellationToken);

        public Result<ICSharpTypeSymbol> UInt32<T>(T source, CancellationToken cancellationToken = default)
            where T : ISourceLocatable
            => compilation.GetTypeSymbol("System.UInt32", source, cancellationToken);

        public Result<ICSharpTypeSymbol> UInt64<T>(T source, CancellationToken cancellationToken = default)
            where T : ISourceLocatable
            => compilation.GetTypeSymbol("System.UInt64", source, cancellationToken);

        public Result<ICSharpTypeSymbol> Int8<T>(T source, CancellationToken cancellationToken = default)
            where T : ISourceLocatable
            => compilation.GetTypeSymbol("System.Int8", source, cancellationToken);

        public Result<ICSharpTypeSymbol> Int16<T>(T source, CancellationToken cancellationToken = default)
            where T : ISourceLocatable
            => compilation.GetTypeSymbol("System.Int16", source, cancellationToken);

        public Result<ICSharpTypeSymbol> Int32<T>(T source, CancellationToken cancellationToken = default)
            where T : ISourceLocatable
            => compilation.GetTypeSymbol("System.Int32", source, cancellationToken);

        public Result<ICSharpTypeSymbol> Int64<T>(T source, CancellationToken cancellationToken = default)
            where T : ISourceLocatable
            => compilation.GetTypeSymbol("System.Int64", source, cancellationToken);

        public Result<ICSharpTypeSymbol> IEnumerableOfT<T>(T source, CancellationToken cancellationToken = default)
            where T : ISourceLocatable
            => compilation.GetTypeSymbol("System.Collections.Generic.IEnumerable`1", source, cancellationToken);

        public Result<ICSharpTypeSymbol> CXChildrenAttribute<T>(T source, CancellationToken cancellationToken = default)
            where T : ISourceLocatable
            => compilation.GetTypeSymbol("ComponentDesigner.CXChildrenAttribute", source, cancellationToken);
    }
}