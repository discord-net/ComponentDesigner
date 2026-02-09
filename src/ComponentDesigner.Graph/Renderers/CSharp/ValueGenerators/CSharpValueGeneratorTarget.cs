using ComponentDesigner.Nodes;
using ComponentDesigner.Parser;

namespace ComponentDesigner;

public record CSharpValueGeneratorTarget(
    CXTextSpan TextSpan,
    CXValue? Value
)
{
    public sealed record ComponentProperty(
        ComponentPropertyValue PropertyValue
    ) : CSharpValueGeneratorTarget(PropertyValue.TextSpan, PropertyValue.CXValue);

    public static implicit operator CSharpValueGeneratorTarget(ComponentPropertyValue propertyValue)
        => new ComponentProperty(propertyValue);
}