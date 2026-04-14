namespace ComponentDesigner.Nodes;

[Flags]
public enum ComponentTargetType : byte
{
    Modal = 1 << 0,
    Message = 1 << 1,
    
    Any = byte.MaxValue
}