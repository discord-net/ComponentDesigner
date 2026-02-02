namespace Discord.CX;

public static class CompilationProviderExtension
{
    extension(ICompilationProvider compilation)
    {
        public ICSharpTypeSymbol BooleanType => compilation.GetTypeFromQualifiedName("System.Boolean");
        public ICSharpTypeSymbol Int32 => compilation.GetTypeFromQualifiedName("System.Int32");
    }
}