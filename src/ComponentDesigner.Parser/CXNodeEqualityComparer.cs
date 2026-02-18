using System.Collections.Generic;
using System.Linq;
using ComponentDesigner.Parser.Util;
using ComponentDesigner.Util;

namespace ComponentDesigner.Parser;

/// <summary>
///     Represents an equality comparer for <see cref="ICXNode"/>.
/// </summary>
/// <param name="flags">
///     The flags used to compare equality between <see cref="ICXNode"/>.
/// </param>
public sealed class CXNodeEqualityComparer(
    SyntaxEqualityFlags flags = SyntaxEqualityFlags.All
) : IEqualityComparer<ICXNode>
{
    /// <summary>
    ///     The default equality comparer instance.
    /// </summary>
    public static readonly CXNodeEqualityComparer Default = new();

    /// <inheritdoc/>
    public bool Equals(ICXNode? x, ICXNode? y)
    {
        if (x is null) return y is null;
        if (y is null) return false;
        
        if (ReferenceEquals(x, y)) return true;

        if (
            (flags & SyntaxEqualityFlags.CompareSourceDocument) != 0 &&
            !ReferenceEquals(x.Document, y.Document)
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
            !x.TextSpan.Equals(y.TextSpan)
        ) return false;

        return (x, y) switch
        {
            (CXToken t1, CXToken t2) =>
                t1.Kind == t2.Kind &&
                t1.RawValue == t2.RawValue,
            (CXNode n1, CXNode n2) =>
                n1.Slots.SequenceEqual(n2.Slots, this),
            
            // 'CXNode' should handle this case, but add it just in case.
            (ICXCollection c1, ICXCollection c2) => 
                c1.ToList().SequenceEqual(c2.ToList(), this),
            
            // no matching types, they don't equal
            _ => false
        };
    }

    /// <inheritdoc/>
    public int GetHashCode(ICXNode obj)
    {
        var hash = 0;

        if ((flags & SyntaxEqualityFlags.CompareLocation) != 0)
            hash = Hash.Combine(hash, obj.TextSpan);

        if ((flags & SyntaxEqualityFlags.CompareTrivia) != 0)
            hash = Hash.Combine(hash, obj.LeadingTrivia, obj.TrailingTrivia);

        if ((flags & SyntaxEqualityFlags.CompareDiagnostics) != 0)
            hash = Hash.Combine(hash, obj.Diagnostics.Aggregate(0, Hash.Combine));

        return Hash.Combine(
            hash,
            obj switch
            {
                CXToken token => Hash.Combine(token.Kind, token.RawValue),
                CXNode node => node.Slots.Aggregate(0, Hash.Combine),
                ICXCollection col => col.ToList().Aggregate(0, Hash.Combine),
                _ => 0
            }
        );
    }
}