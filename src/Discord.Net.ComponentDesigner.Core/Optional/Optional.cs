namespace Discord.CX;

public readonly struct Optional<T> :
    IEquatable<Optional<T>>,
    IEquatable<T>
{
    public T Value
        => IsSpecified ? _value : throw new InvalidOperationException("Optional doesn't contain a value");

    public bool IsSpecified { get; }

    private readonly T _value;

    public Optional()
    {
        IsSpecified = false;
        _value = default!;
    }

    public Optional(T value)
    {
        IsSpecified = true;
        _value = value;
    }

    public bool Equals(Optional<T> other)
        => IsSpecified == other.IsSpecified && (
            !IsSpecified || EqualityComparer<T>.Default.Equals(_value, other._value)
        );

    public bool Equals(T other)
        => IsSpecified && EqualityComparer<T>.Default.Equals(_value, other);
    
    
}