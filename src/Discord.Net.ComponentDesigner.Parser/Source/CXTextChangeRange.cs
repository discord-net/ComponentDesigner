using System;
using System.Collections.Generic;
using Discord.CX.Util;

namespace Discord.CX.Parser;

public readonly struct CXTextChangeRange : IEquatable<CXTextChangeRange>
{
    public CXTextSpan Span { get; }

    public int NewLength { get; }

    public CXTextChangeRange(
        CXTextSpan span,
        int newLength
    )
    {
        if (newLength < 0)
            throw new ArgumentOutOfRangeException(nameof(newLength));

        Span = span;
        NewLength = newLength;
    }

    public bool Equals(CXTextChangeRange other)
        => other.Span == Span && other.NewLength == NewLength;

    public override bool Equals(object? obj)
        => obj is CXTextChangeRange other && Equals(other);

    public override int GetHashCode()
        => Hash.Combine(Span, NewLength);

    public static bool operator ==(CXTextChangeRange left, CXTextChangeRange right)
        => left.Equals(right);

    public static bool operator !=(CXTextChangeRange left, CXTextChangeRange right)
        => !left.Equals(right);

    public static IReadOnlyList<CXTextChangeRange> NoChanges { get; } = [];

    public static CXTextChangeRange Collapse(IEnumerable<CXTextChangeRange> changes)
    {
        var diff = 0;
        var start = int.MaxValue;
        var end = 0;

        foreach (var change in changes)
        {
            diff += change.NewLength - change.Span.Length;

            if (change.Span.Start < start)
            {
                start = change.Span.Start;
            }

            if (change.Span.End > end)
            {
                end = change.Span.End;
            }
        }

        if (start > end)
        {
            // there were no changes.
            return default(CXTextChangeRange);
        }

        var combined = CXTextSpan.FromBounds(start, end);
        var newLen = combined.Length + diff;

        return new CXTextChangeRange(combined, newLen);
    }

    public override string ToString()
    {
        return $"TextChangeRange(Span={Span}, NewLength={NewLength})";
    }
}