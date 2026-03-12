namespace ComponentDesigner;

public static class CompilationProviderExtension
{
    extension(ICompilationProvider compilation)
    {
        public ICSharpTypeSymbol? Boolean => compilation.GetTypeFromQualifiedName("System.Boolean");
        public ICSharpTypeSymbol? String => compilation.GetTypeFromQualifiedName("System.String");
        public ICSharpTypeSymbol? UInt8 => compilation.GetTypeFromQualifiedName("System.UInt8");
        public ICSharpTypeSymbol? UInt16 => compilation.GetTypeFromQualifiedName("System.UInt16");
        public ICSharpTypeSymbol? UInt32 => compilation.GetTypeFromQualifiedName("System.UInt32");
        public ICSharpTypeSymbol? UInt64 => compilation.GetTypeFromQualifiedName("System.UInt64");
        public ICSharpTypeSymbol? Int8 => compilation.GetTypeFromQualifiedName("System.Int8");
        public ICSharpTypeSymbol? Int16 => compilation.GetTypeFromQualifiedName("System.Int16");
        public ICSharpTypeSymbol? Int32 => compilation.GetTypeFromQualifiedName("System.Int32");
        public ICSharpTypeSymbol? Int64 => compilation.GetTypeFromQualifiedName("System.Int64");
        public ICSharpTypeSymbol? IEnumerableOfT => compilation.GetTypeFromQualifiedName("System.Collections.Generic.IEnumerable`1");
        public ICSharpTypeSymbol? CXChildrenAttribute => compilation.GetTypeFromQualifiedName("ComponentDesigner.CXChildrenAttribute");
    }
}