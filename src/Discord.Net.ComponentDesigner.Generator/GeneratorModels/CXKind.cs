using System;

namespace Discord.CX;

[Flags]
public enum CXKind
{
    Unknown = 0,
    
    Modal = 1 << 0,
    Message = 1 << 1,
    
    Any = Modal | Message
}