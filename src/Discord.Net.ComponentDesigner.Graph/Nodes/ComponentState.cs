using System.Diagnostics.CodeAnalysis;
using Discord.CX.Parser;

namespace Discord.CX.Nodes;

public record ComponentState(
    GraphNode GraphNode,
    ICXNode? CXNode
)
{
    public CXTextSpan TextSpan
    {
        get
        {
            if (_textSpan.HasValue) return _textSpan.Value;
            
            var current = this;

            while (current is not null && current.CXNode is null)
                current = current.GraphNode?.Parent?.State;

            return (_textSpan = current?.CXNode?.Span ?? default(CXTextSpan)).Value;
        }
    }

    public CXTextSpan ElementIdentifierTextSpanOrBetter
        => CXNode is CXElement { OpeningTag.Identifier: { } identifier } ? identifier.Span : TextSpan;
    
    public bool HasGraphChildren => GraphNode.HasChildren;

    public IReadOnlyList<GraphNode> Children => (GraphNode.Children as IReadOnlyList<GraphNode>) ?? [..GraphNode.Children];

    [MemberNotNullWhen(true, nameof(CXNode))]
    public bool IsSourcedFromElement => CXNode is CXElement;

    public bool IsRootNode => GraphNode.Parent is null;

    private CXTextSpan? _textSpan;
    
    public ComponentState(ComponentNodeInitializationContext context) : this(context.GraphNode, context.CXNode)
    {
    }
    
    public ComponentPropertyValue GetPropertyValue(ComponentProperty property)
    {
        // TODO perf: can be improved by using a cache of property values

        var attribute = IsSourcedFromElement
            ? ((CXElement)CXNode)
            .Attributes
            .FirstOrDefault(x =>
                property.MatchesName(x.Identifier)
            )
            : null;
        
        GraphNode? node = null;

        if (attribute?.Value is CXValue.Element element)
        {
            node = GraphNode?
                .Attributes
                .FirstOrDefault(x => ReferenceEquals(x.State.CXNode, element.Value));
        }

        CXTextSpan span;

        if (node?.State.CXNode is not null)
            span = node.State.CXNode.Span;
        else if (attribute?.Value is not null)
            span = attribute.Value.Span;
        else if (attribute is not null)
            span = attribute.Span;
        else
            span = TextSpan;
        
        return new ComponentPropertyValue(
            property,
            span,
            attribute,
            node is null ? attribute?.Value : null,
            node
        );
    }
}