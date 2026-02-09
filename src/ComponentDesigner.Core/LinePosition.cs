using ComponentDesigner.Util;

namespace ComponentDesigner;

public readonly struct LinePosition :
    IEquatable<LinePosition>,
    IComparable<LinePosition>
{
    public int Line { get; }
    public int Character { get; }

    public LinePosition(int line, int character)
    {
        if (line < 0) throw new ArgumentOutOfRangeException(nameof(line));

        if (character < 0) throw new ArgumentOutOfRangeException(nameof(character));

        Line = line;
        Character = character;
    }
    
    public static bool operator ==(LinePosition left, LinePosition right)
    {
        return left.Equals(right);
    }
    
    public static bool operator !=(LinePosition left, LinePosition right)
    {
        return !left.Equals(right);
    }

    public bool Equals(LinePosition other)
        => Line == other.Line && Character == other.Character;

    public override bool Equals(object? obj)
        => obj is LinePosition other && Equals(other);

    public override int GetHashCode()
        => Hash.Combine(Line, Character);

    public override string ToString()
        => $"{Line},{Character}";
    
    public int CompareTo(LinePosition other)
    {
        var num = Line.CompareTo(other.Line);
        if (num == 0)
        {
            return Character.CompareTo(other.Character);
        }

        return num;
    }

    public static bool operator >(LinePosition left, LinePosition right)
    {
        return left.CompareTo(right) > 0;
    }

    public static bool operator >=(LinePosition left, LinePosition right)
    {
        return left.CompareTo(right) >= 0;
    }

    public static bool operator <(LinePosition left, LinePosition right)
    {
        return left.CompareTo(right) < 0;
    }

    public static bool operator <=(LinePosition left, LinePosition right)
    {
        return left.CompareTo(right) <= 0;
    }
}