using Discord.CX.Parser;

namespace Discord.CX.Nodes;

public readonly record struct ComponentPropertyValue(
    ComponentProperty Property,
    CXTextSpan TextSpan,
    CXAttribute? Attribute,
    CXValue? Value,
    GraphNode? GraphNode
)
{
    public bool HasValue => Value is not null || GraphNode is not null;

    public bool IsSpecified => Attribute is not null || HasValue;
}