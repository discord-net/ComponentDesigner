using System;

namespace Discord.CX.Parser;

public readonly record struct CXTextSpan(
    int Start,
    int Length
) : IComparable<CXTextSpan>
{
    /// <summary>
    ///     End of the span.
    /// </summary>
    public int End => Start + Length;


    /// <summary>
    ///     Determines whether the span is empty.
    /// </summary>
    public bool IsEmpty => Length is 0;

    /// <summary>
    ///     Determines whether the position lies within the span.
    /// </summary>
    /// <param name="position">The position to check.</param>
    /// <returns>
    ///     <c>true</c> if the position is greater than or equal to Start and strictly less 
    ///     than End, otherwise <c>false</c>.
    /// </returns>
    public bool Contains(int position)
        => unchecked((uint)(position - Start) < (uint)Length);

    /// <summary>
    ///     Determines whether <paramref name="span"/> falls completely within this span.
    /// </summary>
    /// <param name="span">The span to check.</param>
    /// <returns>
    ///     <c>true</c> if the specified span falls completely within this span; otherwise <c>false</c>.
    /// </returns>
    public bool Contains(CXTextSpan span)
        => span.Start >= Start && span.End <= this.End;

    /// <summary>
    ///     Determines whether <paramref name="span"/> overlaps this span. Two spans are considered to overlap 
    ///     if they have positions in common and neither is empty. Empty spans do not overlap with any 
    ///     other span.
    /// </summary>
    /// <param name="span">The span to check.</param>
    /// <returns>
    ///     <c>true</c> if the spans overlap, otherwise <c>false</c>.
    /// </returns>
    public bool OverlapsWith(CXTextSpan span)
    {
        var overlapStart = Math.Max(Start, span.Start);
        var overlapEnd = Math.Min(this.End, span.End);

        return overlapStart < overlapEnd;
    }

    /// <summary>
    ///     Returns the overlap with the given span, or null if there is no overlap.
    /// </summary>
    /// <param name="span">The span to check.</param>
    /// <returns>The overlap of the spans, or null if the overlap is empty.</returns>
    public CXTextSpan? Overlap(CXTextSpan span)
    {
        var overlapStart = Math.Max(Start, span.Start);
        var overlapEnd = Math.Min(this.End, span.End);

        return overlapStart < overlapEnd
            ? CXTextSpan.FromBounds(overlapStart, overlapEnd)
            : (CXTextSpan?)null;
    }

    /// <summary>
    ///     Determines whether <paramref name="span"/> intersects this span. Two spans are considered to 
    ///     intersect if they have positions in common or the end of one span 
    ///     coincides with the start of the other span.
    /// </summary>
    /// <param name="span">The span to check.</param>
    /// <returns>
    ///     <c>true</c> if the spans intersect, otherwise <c>false</c>.
    /// </returns>
    public bool IntersectsWith(CXTextSpan span)
        => span.Start <= this.End && span.End >= Start;

    /// <summary>
    ///     Determines whether <paramref name="position"/> intersects this span. 
    ///     A position is considered to intersect if it is between the start and
    ///     end positions (inclusive) of this span.
    /// </summary>
    /// <param name="position">The position to check.</param>
    /// <returns>
    ///     <c>true</c> if the position intersects, otherwise <c>false</c>.
    /// </returns>
    public bool IntersectsWith(int position)
        => unchecked((uint)(position - Start) <= (uint)Length);

    /// <summary>
    ///     Returns the intersection with the given span, or null if there is no intersection.
    /// </summary>
    /// <param name="span">The span to check.</param>
    /// <returns>
    ///     The intersection of the spans, or null if the intersection is empty.
    /// </returns>
    public CXTextSpan? Intersection(CXTextSpan span)
    {
        var intersectStart = Math.Max(Start, span.Start);
        var intersectEnd = Math.Min(this.End, span.End);

        return intersectStart <= intersectEnd
            ? CXTextSpan.FromBounds(intersectStart, intersectEnd)
            : (CXTextSpan?)null;
    }

    /// <summary>
    /// Creates a new <see cref="CXTextSpan"/> from <paramref name="start" /> and <paramref
    /// name="end"/> positions as opposed to a position and length.
    /// 
    /// The returned TextSpan contains the range with <paramref name="start"/> inclusive, 
    /// and <paramref name="end"/> exclusive.
    /// </summary>
    public static CXTextSpan FromBounds(int start, int end)
    {
        if (start < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(start), "must be a non-negative value");
        }

        if (end < start)
        {
            throw new ArgumentOutOfRangeException(nameof(end),
                string.Format("start must be less than or equal to end", start, end));
        }

        return new CXTextSpan(start, end - start);
    }

    /// <summary>
    ///     Provides a string representation for <see cref="CXTextSpan"/>.
    ///     This representation uses "half-open interval" notation, indicating the endpoint character is not included.
    ///     Example: <c>[10..20)</c>, indicating the text starts at position 10 and ends at position 20 not included.
    /// </summary>
    public override string ToString()
        => $"[{Start}..{End})";

    /// <summary>
    ///     Compares current instance of <see cref="CXTextSpan"/> with another.
    /// </summary>
    public int CompareTo(CXTextSpan other)
    {
        var diff = Start - other.Start;
        if (diff != 0)
        {
            return diff;
        }

        return Length - other.Length;
    }
}