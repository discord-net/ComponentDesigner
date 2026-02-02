using Discord.CX.Util;

namespace Discord.CX;

public readonly struct LinePositionSpan :
    IEquatable<LinePositionSpan>
{
    public LinePosition Start { get; }
    public LinePosition End { get; }

    public LinePositionSpan(LinePosition start, LinePosition end)
    {
        if (end < start) throw new ArgumentOutOfRangeException(nameof(end), "end must appear after start");

        Start = start;
        End = end;
    }

    public bool Equals(LinePositionSpan other)
        => other.Start == Start && other.End == End;

    public override bool Equals(object? obj)
        => obj is LinePositionSpan other && Equals(other);

    public override int GetHashCode()
        => Hash.Combine(Start, End);
    
    public static bool operator ==(LinePositionSpan left, LinePositionSpan right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(LinePositionSpan left, LinePositionSpan right)
    {
        return !left.Equals(right);
    }

    public override string ToString()
    {
        return $"({Start})-({End})";
    }
}