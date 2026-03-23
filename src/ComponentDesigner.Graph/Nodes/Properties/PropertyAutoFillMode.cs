namespace ComponentDesigner.Nodes;

[Flags]
public enum ComponentPropertyFlags
{
    None = 0,
    FromChildren = 1 << 0,
}