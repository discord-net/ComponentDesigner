namespace ComponentDesigner;

public static class CompilationProviderExtension
{
    extension(ICompilationProvider compilation)
    {
        public ICSharpTypeSymbol? Boolean => compilation.GetTypeFromQualifiedName("System.Boolean");
        public ICSharpTypeSymbol? String => compilation.GetTypeFromQualifiedName("System.String");
        public ICSharpTypeSymbol? Int32 => compilation.GetTypeFromQualifiedName("System.Int32");
        public ICSharpTypeSymbol? UInt64 => compilation.GetTypeFromQualifiedName("System.UInt64");
        public ICSharpTypeSymbol? IEnumerableOfT => compilation.GetTypeFromQualifiedName("System.Collections.Generic.IEnumerable`1");
        public ICSharpTypeSymbol? CXChildrenAttribute => compilation.GetTypeFromQualifiedName("ComponentDesigner.CXChildrenAttribute");
    }
}