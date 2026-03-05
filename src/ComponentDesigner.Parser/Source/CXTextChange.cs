using System;
using System.Collections.Generic;
using ComponentDesigner;
using ComponentDesigner.Parser.Util;
using ComponentDesigner.Util;

namespace ComponentDesigner.Parser;

public readonly struct CXTextChange : IEquatable<CXTextChange>
{
    public CXTextSpan Span { get; }
    
    public string? NewText { get; }

    public CXTextChange(CXTextSpan span, string? newText)
    {
        Span = span;
        NewText = newText ?? throw new ArgumentNullException(nameof(newText));
    }

    public override string ToString()
        => $"{{ {Span}, \"{NewText}\" }}";

    public override bool Equals(object? obj)
        => obj is CXTextChange other && Equals(other);

    public bool Equals(CXTextChange other)
        => Span == other.Span && EqualityComparer<string>.Default.Equals(NewText!, other.NewText!);

    public override int GetHashCode()
        => Hash.Combine(Span, NewText);

    public static bool operator ==(CXTextChange left, CXTextChange right)
        => left.Equals(right);

    public static bool operator !=(CXTextChange left, CXTextChange right)
        => !left.Equals(right);
    
    public static implicit operator CXTextChangeRange(CXTextChange change)
        => new (change.Span, change.NewText?.Length ?? 0);

    public static IReadOnlyList<CXTextChange> NoChanges { get; } = [];
}