using System;

namespace Discord.CX.Parser;

[Flags]
public enum SyntaxEqualityFlags : byte
{
    CompareTrivia = 1 << 0,
    CompareLocation = 1 << 1,
    CompareSourceDocument = 1 << 2,
    CompareFlags = 1 << 3,
    CompareDiagnostics = 1 << 4,
    
    All = byte.MaxValue
}