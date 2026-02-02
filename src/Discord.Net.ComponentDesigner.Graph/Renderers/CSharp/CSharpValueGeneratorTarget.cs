using Discord.CX.Nodes;
using Discord.CX.Parser;

namespace Discord.CX;

public record CSharpValueGeneratorTarget(
    CXTextSpan TextSpan,
    CXValue? Value
)
{
    public sealed record ComponentProperty(
        ComponentPropertyValue PropertyValue
    ) : CSharpValueGeneratorTarget(PropertyValue.TextSpan, PropertyValue.Value);

    public static implicit operator CSharpValueGeneratorTarget(ComponentPropertyValue propertyValue)
        => new ComponentProperty(propertyValue);
}