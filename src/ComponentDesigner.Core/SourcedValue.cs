using System.Runtime.CompilerServices;

namespace ComponentDesigner;

public readonly record struct SourcedValue<T>(
    CXTextSpan TextSpan,
    T Value
) : ISourceLocatable
{
    public static implicit operator T(SourcedValue<T> self) => self.Value;

    public override string ToString()
        => Value?.ToString() ?? string.Empty;
}

public static class SourcedValueExtensions
{
    extension<T>(T value)
    {
        public SourcedValue<T> SourcedAt(CXTextSpan textSpan) => new(textSpan, value);
    }
}