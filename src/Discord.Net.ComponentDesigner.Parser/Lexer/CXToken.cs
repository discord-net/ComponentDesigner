using System;
using System.Collections.Generic;
using System.Linq;

namespace Discord.CX.Parser;

/// <summary>
///     An AST node representing a terminal token within the CX syntax.
/// </summary>
/// <param name="Kind">The kind of the token.</param>
/// <param name="LeadingTrivia">The leading trivia of the token.</param>
/// <param name="TrailingTrivia">The trailing trivia of the token.</param>
/// <param name="Flags">The flags of the token.</param>
/// <param name="RawValue">The raw value of the token.</param>
/// <param name="Diagnostics">The diagnostics for the token.</param>
public sealed record CXToken(
    CXTokenKind Kind,
    LexedCXTrivia LeadingTrivia,
    LexedCXTrivia TrailingTrivia,
    CXTokenFlags Flags,
    string RawValue,
    string? Value = null,
    params IReadOnlyList<CXDiagnosticDescriptor> Diagnostics
) : ICXNode
{
    /// <summary>
    ///     Gets the value representing this <see cref="CXToken"/>.
    /// </summary>
    /// <remarks>
    ///     This value may not line up with the value within the source, for example if the token contains
    ///     escape sequences, those sequences will be parsed into their respective codes in this value. Use
    ///     <see cref="RawValue"/> to access the raw, unaltered value from the source.
    /// </remarks>
    public string Value { get; init; } = Value ?? RawValue;

    /// <summary>
    ///     Gets the interpolation index of this token.
    /// </summary>
    /// <remarks>
    ///     If this tokens <see cref="Kind"/> is not an <see cref="CXTokenKind.Interpolation"/>, <see langword="null"/>
    ///     is returned.
    /// </remarks>
    public int? InterpolationIndex
        => Document is null || Kind is not CXTokenKind.Interpolation ? null : Document.GetInterpolationIndex(this);

    /// <summary>
    ///     Gets the full character width of this token. 
    /// </summary>
    public int Width => IsMissing
        ? 0
        : LeadingTrivia.CharacterLength + RawValue.Length + TrailingTrivia.CharacterLength;

    public CXTextSpan Span => new(this.Offset + LeadingTrivia.CharacterLength, IsMissing ? 0 : RawValue.Length);
    public CXTextSpan FullSpan => new(this.Offset, Width);

    /// <inheritdoc/>
    public CXNode? Parent { get; set; }

    /// <inheritdoc/>
    public CXDocument? Document => Parent?.Document;

    /// <inheritdoc/>
    public bool HasErrors
        => IsInvalid ||
           IsMissing ||
           Diagnostics.Any(x => x.Severity is DiagnosticSeverity.Error);

    /// <summary>
    ///     Gets whether this token is missing from the underlying <see cref="CXSourceText"/>.
    /// </summary>
    public bool IsMissing => (Flags & CXTokenFlags.Missing) != 0;

    /// <summary>
    ///     Gets whether this token has a zero character width.
    /// </summary>
    public bool IsZeroWidth => Span.IsEmpty;

    /// <summary>
    ///     Gets whether this token is an <see cref="CXTokenKind.Invalid"/> kind.
    /// </summary>
    public bool IsInvalid => Kind is CXTokenKind.Invalid;

    /// <summary>
    ///     Creates a new synthetic token.
    /// </summary>
    /// <param name="kind">The kind of the synthetic token.</param>
    /// <param name="span">The <see cref="CXTextSpan"/> of the synthetic token.</param>
    /// <param name="flags">The flags of the synthetic token.</param>
    /// <param name="rawValue">The raw value of the synthetic token.</param>
    /// <param name="value">The value of the synthetic token.</param>
    /// <param name="diagnostics">The diagnostics of the synthetic token.</param>
    /// <returns>The newly created synthetic token.</returns>
    public static CXToken CreateSynthetic(
        CXTokenKind kind,
        CXTextSpan? span = null,
        CXTokenFlags? flags = null,
        string? rawValue = null,
        string? value = null,
        IEnumerable<CXDiagnosticDescriptor>? diagnostics = null
    )
    {
        return new CXToken(
            kind,
            LexedCXTrivia.Empty,
            LexedCXTrivia.Empty,
            CXTokenFlags.Synthetic | (flags ?? CXTokenFlags.None),
            rawValue ?? string.Empty,
            value,
            [..diagnostics ?? []]
        );
    }

    /// <summary>
    ///     Creates a new <see cref="CXToken"/> with the <see cref="CXTokenFlags.Missing"/> flag set.
    /// </summary>
    /// <param name="kind">The kind of the token to create.</param>
    /// <param name="diagnostics">The diagnostics of the token.</param>
    /// <returns>The newly created token.</returns>
    public static CXToken CreateMissing(
        CXTokenKind kind,
        params IEnumerable<CXDiagnosticDescriptor> diagnostics
    ) => CreateMissing(kind, string.Empty, diagnostics: diagnostics);

    /// <summary>
    ///     Creates a new <see cref="CXToken"/> with the <see cref="CXTokenFlags.Missing"/> flag set.
    /// </summary>
    /// <param name="kind">The kind of the token to create.</param>
    /// <param name="rawValue">The value of the token.</param>
    /// <param name="leadingTrivia">The leading trivia of the token to create.</param>
    /// <param name="trailingTrivia">The trailing trivia of the token to create.</param>
    /// <param name="diagnostics">The diagnostics of the token.</param>
    /// <returns>The newly created token.</returns>
    public static CXToken CreateMissing(
        CXTokenKind kind,
        string rawValue,
        LexedCXTrivia? leadingTrivia = null,
        LexedCXTrivia? trailingTrivia = null,
        params IEnumerable<CXDiagnosticDescriptor> diagnostics
    ) => new(
        kind,
        leadingTrivia ?? LexedCXTrivia.Empty,
        trailingTrivia ?? LexedCXTrivia.Empty,
        Flags: CXTokenFlags.Missing,
        RawValue: rawValue,
        Diagnostics: [..diagnostics]
    );

    /// <inheritdoc/>
    public override string ToString() => ToString(false, false);

    /// <inheritdoc/>
    public string ToString(bool includeLeadingTrivia, bool includeTrailingTrivia)
    {
        if (IsMissing) return string.Empty;
        
        return (includeLeadingTrivia, includeTrailingTrivia) switch
        {
            (false, false) => RawValue,
            (true, true) => $"{LeadingTrivia}{RawValue}{TrailingTrivia}",
            (false, true) => $"{RawValue}{TrailingTrivia}",
            (true, false) => $"{LeadingTrivia}{RawValue}"
        };
    }

    /// <inheritdoc/>
    public bool Equals(CXToken? other)
        => CXNodeEqualityComparer.Default.Equals(this, other);

    /// <inheritdoc/>
    public bool Equals(ICXNode? other)
        => other is CXToken token && Equals(token);

    /// <inheritdoc/>
    public override int GetHashCode()
        => CXNodeEqualityComparer.Default.GetHashCode(this);

    /// <inheritdoc/>
    int ICXNode.GraphWidth => 0;

    /// <inheritdoc/>
    IReadOnlyList<ICXNode> ICXNode.Slots => [];

    IReadOnlyList<CXDiagnosticDescriptor> ICXNode.DiagnosticDescriptors
    {
        get => Diagnostics;
        init => Diagnostics = value;
    }

    object ICloneable.Clone() => this with { };

    CXDiagnostic ICXNode.CreateDiagnostic(CXDiagnosticDescriptor descriptor)
        => new(descriptor, Span);

    void ICXNode.ResetCachedState()
    {
    }
}