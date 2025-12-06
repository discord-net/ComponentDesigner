using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Discord.CX.Parser;

public sealed record CXToken(
    CXTokenKind Kind,
    LexedCXTrivia LeadingTrivia,
    LexedCXTrivia TrailingTrivia,
    CXTokenFlags Flags,
    string Value,
    params IReadOnlyList<CXDiagnostic> Diagnostics
) : ICXNode
{
    public int? InterpolationIndex
        => Document is null || Kind is not CXTokenKind.Interpolation ? null : Document.GetInterpolationIndex(this); 
    
    public int Width => LeadingTrivia.Length + Value.Length + TrailingTrivia.Length;
    
    public int Offset
    {
        get
        {
            if (Parent is null) return 0;

            var parentOffset = Parent.Offset;
            var parentSlotIndex = GetParentSlotIndex();

            return parentSlotIndex switch
            {
                -1 => throw new InvalidOperationException(),
                0 => parentOffset,
                _ => Parent.Slots[parentSlotIndex - 1].Value switch
                {
                    CXNode sibling => sibling.Offset + sibling.Width,
                    CXToken token => token.Offset + token.Width,
                    _ => throw new InvalidOperationException()
                }
            };

            int GetParentSlotIndex()
            {
                if (Parent is null) return -1;

                for (var i = 0; i < Parent.Slots.Count; i++)
                    if (Parent.Slots[i] == this)
                        return i;

                return -1;
            }
        }
    }

    public TextSpan Span => new(Offset + LeadingTrivia.Length, Value.Length);
    public TextSpan FullSpan => new(Offset, Width);

    public CXNode? Parent { get; set; }

    public bool HasErrors
        => _hasErrors ??= (
            Diagnostics.Any(x => x.Severity is DiagnosticSeverity.Error) ||
            IsInvalid ||
            IsMissing
        );

    public bool IsMissing => (Flags & CXTokenFlags.Missing) != 0;

    public bool IsZeroWidth => Span.IsEmpty;

    public bool IsInvalid => Kind is CXTokenKind.Invalid;

    private bool? _hasErrors;

    public static CXToken CreateSynthetic(
        CXTokenKind kind,
        TextSpan? span = null,
        CXTokenFlags? flags = null,
        string? value = null,
        IEnumerable<CXDiagnostic>? diagnostics = null
    )
    {
        return new CXToken(
            kind,
            LexedCXTrivia.Empty, 
            LexedCXTrivia.Empty, 
            CXTokenFlags.Synthetic | (flags ?? CXTokenFlags.None),
            value ?? string.Empty,
            [..diagnostics ?? []]
        );
    }

    public static CXToken CreateMissing(
        CXTokenKind kind,
        params IEnumerable<CXDiagnostic> diagnostics
    ) => CreateMissing(kind, string.Empty, diagnostics);

    public static CXToken CreateMissing(
        CXTokenKind kind,
        string value,
        params IEnumerable<CXDiagnostic> diagnostics
    ) => new(
        kind,
        LexedCXTrivia.Empty, 
        LexedCXTrivia.Empty, 
        Flags: CXTokenFlags.Missing,
        Value: value,
        Diagnostics: [..diagnostics]
    );

    public CXDoc? Document => Parent?.Document;

    public void ResetCachedState()
    {
        _hasErrors = null;
    }

    public override string ToString() => ToString(false, false);
    public string ToFullString() => ToString(true, true);

    public string ToString(bool includeLeadingTrivia, bool includeTrailingTrivia)
        => (includeLeadingTrivia, includeTrailingTrivia) switch
        {
            (false, false) => Value,
            (true, true) => $"{LeadingTrivia}{Value}{TrailingTrivia}",
            (false, true) => $"{Value}{TrailingTrivia}",
            (true, false) => $"{LeadingTrivia}{Value}"
        };

    public bool Equals(CXToken? other)
    {
        if (other is null) return false;

        if (ReferenceEquals(this, other)) return true;

        return
            Kind == other.Kind &&
            Span.Equals(other.Span) &&
            LeadingTrivia.Equals(other.LeadingTrivia) &&
            TrailingTrivia.Equals(other.TrailingTrivia) &&
            Flags == other.Flags &&
            Diagnostics.SequenceEqual(other.Diagnostics);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Diagnostics.Aggregate(0, (a, b) => (a * 397) ^ b.GetHashCode());
            hashCode = (hashCode * 397) ^ (int)Kind;
            hashCode = (hashCode * 397) ^ Span.GetHashCode();
            hashCode = (hashCode * 397) ^ LeadingTrivia.GetHashCode();
            hashCode = (hashCode * 397) ^ TrailingTrivia.GetHashCode();
            hashCode = (hashCode * 397) ^ (int)Flags;
            return hashCode;
        }
    }

    int ICXNode.GraphWidth => 0;
    IReadOnlyList<CXNode.ParseSlot> ICXNode.Slots => [];
}