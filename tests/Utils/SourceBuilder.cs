using System.Text;
using ComponentDesigner;
using Microsoft.CodeAnalysis.Text;

namespace UnitTests;

public sealed class SourceBuilder
{
    public StringBuilder StringBuilder { get; } = new();
    public List<CXTextSpan> Interpolations { get; } = [];
        
    public SourceBuilder AddSource(string source)
    {
        StringBuilder.Append(source);
        return this;
    }

    public SourceBuilder AddInterpolation(string interpolation)
    {
        var actual = $"{{{interpolation}}}";
            
        var span = new CXTextSpan(StringBuilder.Length, actual.Length);
        Interpolations.Add(span);
            
        return AddSource(actual);
    }
}