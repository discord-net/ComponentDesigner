using System;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;

namespace Discord.CX.Parser;

public interface ICXNode : IEquatable<ICXNode>
{
    TextSpan FullSpan { get; }
    TextSpan Span { get; }

    int Width { get; }

    int GraphWidth { get; }

    bool HasErrors { get; }

    IReadOnlyList<CXDiagnostic> Diagnostics { get; }

    CXNode? Parent { get; internal set; }

    IReadOnlyList<CXNode.ParseSlot> Slots { get; }
    
    CXDoc? Document { get; }

    LexedCXTrivia LeadingTrivia { get; }
    LexedCXTrivia TrailingTrivia { get; }
    
    void ResetCachedState();

    string ToString(bool includeLeadingTrivia, bool includeTrailingTrivia);
}
