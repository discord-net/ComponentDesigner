using System.Collections.Generic;
using System.Linq;

namespace Discord.CX.Parser;

public sealed class CXNodeEqualityComparer(SyntaxEqualityFlags flags = SyntaxEqualityFlags.All) :
    IEqualityComparer<ICXNode>
{
    public static readonly CXNodeEqualityComparer Default = new();

    public bool Equals(ICXNode x, ICXNode y)
    {
        if (ReferenceEquals(x, y)) return true;

        if (
            (flags & SyntaxEqualityFlags.CompareSourceDocument) != 0 &&
            (!x.Document?.Equals(y.Document) ?? y.Document is not null)
        )
        {
            return false;
        }
        
        if (
            (flags & SyntaxEqualityFlags.CompareTrivia) != 0 &&
            (
                !x.LeadingTrivia.Equals(y.LeadingTrivia) ||
                !x.TrailingTrivia.Equals(y.LeadingTrivia)
            )
        ) return false;

        if (
            (flags & SyntaxEqualityFlags.CompareDiagnostics) != 0 &&
            !x.Diagnostics.SequenceEqual(y.Diagnostics)
        ) return false;

        if (
            (flags & SyntaxEqualityFlags.CompareFlags) != 0 &&
            x is CXToken a && y is CXToken b &&
            a.Flags != b.Flags
        ) return false;

        if (
            (flags & SyntaxEqualityFlags.CompareLocation) != 0 &&
            !x.Span.Equals(y.Span)
        ) return false;

        return x.Equals(y);
    }

    public int GetHashCode(ICXNode obj)
        => obj.GetHashCode();
}