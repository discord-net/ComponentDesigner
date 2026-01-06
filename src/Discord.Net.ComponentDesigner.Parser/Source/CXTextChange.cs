using System;
using System.Collections.Generic;
using Discord.CX.Util;

namespace Discord.CX.Parser;

public readonly struct CXTextChange : IEquatable<CXTextChange>
{
    public CXTextSpan Span { get; }
    
    public string? NewText { get; }

    public CXTextChange(CXTextSpan span, string? newText)
    {
        if (newText is null)
            throw new ArgumentNullException(nameof(newText));

        Span = span;
        NewText = newText;
    }

    public override string ToString()
        => $"{nameof(CXTextChange)}: {{ {Span}, \"{NewText}\" }}";

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