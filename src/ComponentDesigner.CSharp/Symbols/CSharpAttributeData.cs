using Microsoft.CodeAnalysis;

namespace ComponentDesigner.CSharp;

public sealed class CSharpAttributeData : ICSharpAttributeData
{
    public ICSharpTypeSymbol Type => _provider.GetTypeSymbol(_inner.AttributeClass)!;
    
    private readonly CSharpCompilationProvider _provider;
    private readonly AttributeData _inner;

    public CSharpAttributeData(CSharpCompilationProvider provider, AttributeData inner)
    {
        _provider = provider;
        _inner = inner;
    }
    
    public bool Equals(ICSharpAttributeData other)
    {
        throw new NotImplementedException();
    }

}