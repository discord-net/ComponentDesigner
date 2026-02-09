using ComponentDesigner.Util;

namespace ComponentDesigner;

public readonly struct Result<T> :
    IEquatable<Result<T>>
{
    public static readonly Result<T> Empty = new();

    public bool IsEmpty => !HasValue && (_diagnostics is null or { Count: 0 });
    
    public T Value
    {
        get
        {
            if (HasValue) return _value!;

            throw new InvalidOperationException("Result doesn't have a value");
        }
    }
    
    public bool HasValue { get; }

    public IReadOnlyList<Diagnostic> Diagnostics => _diagnostics ?? [];

    private readonly T _value;
    private readonly IReadOnlyList<Diagnostic>? _diagnostics;

    public Result(T value)
    {
        _value = value;
        HasValue = true;
    }
    
    public Result(T value, IReadOnlyList<Diagnostic> diagnostics)
    {
        _value = value;
        HasValue = true;
        _diagnostics = diagnostics;
    }

    public Result()
    {
    }

    public Result(IReadOnlyList<Diagnostic> diagnostics)
    {
        _diagnostics = diagnostics;
    }

    internal Result(T value, bool isSpecified, IReadOnlyList<Diagnostic>? diagnostics)
    {
        _value = value;
        HasValue = isSpecified;
        _diagnostics = diagnostics;
    }

    public Result<T> AddDiagnostics(params IReadOnlyList<Diagnostic> diagnostics)
    {
        var newDiagnostics = _diagnostics is null or { Count: 0 }
            ? diagnostics.Count is 0 ? null : diagnostics
            : diagnostics.Count is 0 ? _diagnostics : [.._diagnostics, ..diagnostics];

        return new(_value, HasValue, newDiagnostics);
    }
    
    public bool Equals(Result<T> other)
        => HasValue == other.HasValue &&
           EqualityComparer<T>.Default.Equals(_value, other._value) &&
           Diagnostics.SequenceEqual(other.Diagnostics);

    public override bool Equals(object? obj)
        => obj is Result<T> other && Equals(other);

    public override int GetHashCode()
        => Hash.Combine(HasValue, _value, _diagnostics?.Aggregate(0, Hash.Combine));

    public static implicit operator Result<T>(T value) => new(value);
    public static implicit operator Result<T>(Diagnostic diagnostic) => new([diagnostic]);
    public static implicit operator Result<T>((T, Diagnostic) tuple) => new(tuple.Item1, [tuple.Item2]);
    public static implicit operator Result<T>((T, IReadOnlyList<Diagnostic>) tuple) => new(tuple.Item1, tuple.Item2);
}