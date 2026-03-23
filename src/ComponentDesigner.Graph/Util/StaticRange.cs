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

    public static bool TryCreateFromProperties(
        ComponentPropertyValue lower,
        ComponentPropertyValue upper,
        out StaticRange range
    )
    {
        var isValidLower = TryGetInt(lower, out var lowerValue);
        var isValidUpper = TryGetInt(upper, out var upperValue);
        range = new(lowerValue, upperValue);
        return isValidLower && isValidUpper;
        
        static bool TryGetInt(ComponentPropertyValue propertyValue, out int? result)
        {
            switch (propertyValue.AsSingle)
            {
                case ComponentPropertyValue.Literal { Value: var str }
                    when int.TryParse(str, out var value):
                case ComponentPropertyValue.Interpolation { Info.ConstantValue: { IsSpecified: true } constant }
                    when int.TryParse(constant.ToString(), out value):
                    result = value;
                    return true;
                case ComponentPropertyValue.None when !propertyValue.IsAttributeNameOnly:
                    result = null;
                    return true;
            }

            result = null;
            return false;
        }
    }

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