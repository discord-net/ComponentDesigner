using Discord.CX.Parser;
using Microsoft.CodeAnalysis.Text;

namespace Discord.CX.Nodes;

public record CXValueGeneratorTarget(
    CXValue? Value,
    TextSpan Span
)
{
    public CXValueGeneratorTarget(CXValue value) : this(value, value.Span)
    {
    }
    
    public sealed record ComponentProperty(
        IComponentPropertyValue Property
    ) : CXValueGeneratorTarget(Property.Value, Property.Span);
}