using System.Diagnostics.CodeAnalysis;
using ComponentDesigner.Nodes;

namespace ComponentDesigner;

public readonly record struct StaticRange(
    int? Lower = null,
    int? Upper = null
)
{
    public bool IsScalarRange => Upper is not null && Upper == Lower;

    [MemberNotNullWhen(true, nameof(Lower), nameof(Upper))]
    public bool IsBoundedRange => Lower is not null && Upper is not null;

    public bool IsEmpty => Lower is null && Upper is null;

    [MemberNotNullWhen(true, nameof(Lower), nameof(Upper))]
    public bool IsInvalid => IsBoundedRange && Lower > Upper;

    public bool IsUnboundedLower => Lower is null;
    public bool IsUnboundedUpper => Upper is null;

    public static readonly StaticRange Empty = new();

    public StaticRange(int value) : this(value, value)
    {
    }

    public bool Contains(StaticRange other)
    {
        if (IsEmpty) return true;

        return
            (Lower is null || Lower <= other.Lower) &&
            (Upper is null || Upper >= other.Upper);
    }

    public bool Contains(int value)
    {
        if (value > Upper || value < Lower) return false;

        return true;
    }

    public bool? Fits(StaticRange other)
    {
        var min = other.Lower;
        var max = other.Upper;

        if (Upper > max || Lower < min) return false;
        
        if (
            other.IsEmpty ||
            (other.Lower is not null && Lower is null) ||
            (other.Upper is not null && Upper is null)
        ) return null;
        
        return (Lower is null || Lower >= other.Lower) && (Upper is null || Upper <= other.Upper);
    }

    public bool? Fits(
        int? lower = null,
        int? upper = null
    ) => Fits((lower, upper));

    public StaticRange WithBoundedLower()
        => this with { Lower = Lower ?? 0 };

    public StaticRange WithBoundedUpper()
        => this with { Upper = Upper ?? 0 };

    public static StaticRange operator +(StaticRange self, int value)
        => new(
            (self.Lower ?? 0) + value,
            (self.Upper ?? 0) + value
        );

    public static StaticRange operator ++(StaticRange self)
        => self + 1;

    public static implicit operator StaticRange(int value) => new(value);
    public static implicit operator StaticRange((int?, int?) tuple) => new(tuple.Item1, tuple.Item2);

    public string ToRangeString()
        => $"{Lower}..{Upper}";

    public override string ToString()
        => (Lower, Upper) switch
        {
            (null, null) => "empty",
            (null, not null) => $"at most {Upper}",
            (not null, null) => $"at least {Lower}",
            (not null, not null) when Lower == Upper => Upper.Value.ToString(),
            (not null, not null) => $"between {Lower} and {Upper}",
        };
}