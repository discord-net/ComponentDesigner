using Discord.CX.Parser;
using Microsoft.CodeAnalysis.Text;

namespace Discord.CX.Nodes;

public interface IComponentPropertyValue
{
    CXTextSpan Span { get; }
    
    CXValue? Value { get; }
    
    GraphNode? GraphNode { get; }
    
    bool IsSpecified { get; }
    
    bool HasValue { get; }
    
    bool IsAttributeValue { get; }
    
    bool RequiresValue { get; }
    
    bool IsOptional { get; }
    
    string PropertyName { get; }
}