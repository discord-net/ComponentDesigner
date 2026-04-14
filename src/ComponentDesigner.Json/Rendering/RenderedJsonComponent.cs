using System.Text.Json.Nodes;

namespace ComponentDesigner.Json;

public sealed record RenderedJsonComponent : RenderedComponent
{
    public JsonNode JsonNode { get; }

    public override string Source => JsonNode.ToJsonString();

    public RenderedJsonComponent(JsonNode jsonNode, ICSharpTypeSymbol? type = null) : base(type)
    {
        JsonNode = jsonNode;
    }
}