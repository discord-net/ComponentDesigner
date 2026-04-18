using ComponentDesigner.Nodes;

namespace ComponentDesigner;

public interface IGraphOptions
{
    bool AllowAutoRows { get; }
    bool AllowAutoTextDisplays { get; }
    ComponentTargetType Target { get; }
}
