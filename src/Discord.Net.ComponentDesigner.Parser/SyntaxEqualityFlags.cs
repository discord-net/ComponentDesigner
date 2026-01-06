using System;

namespace Discord.CX.Parser;

/// <summary>
///     A bit-wise flag that determines how equality checks are performed against <see cref="ICXNode"/>s.
/// </summary>
[Flags]
public enum SyntaxEqualityFlags : byte
{
    /// <summary>
    ///     Indicates that <see cref="CXTrivia"/> should be compared for equality between <see cref="ICXNode"/>s.
    /// </summary>
    CompareTrivia = 1 << 0,

    /// <summary>
    ///     Indicates that locational <see cref="CXTextSpan"/>s should be compared for equality between
    ///     <see cref="ICXNode"/>.
    /// </summary>
    CompareLocation = 1 << 1,

    /// <summary>
    ///     Indicates that the root <see cref="CXDocument"/> should be compared for equality between <see cref="ICXNode"/>s.
    /// </summary>
    CompareSourceDocument = 1 << 2,

    /// <summary>
    ///     Indicates that node-specific flags (like <see cref="CXTokenFlags"/>) should be compared for equality between
    ///     <see cref="ICXNode"/>.
    /// </summary>
    CompareFlags = 1 << 3,

    /// <summary>
    ///     Indicates that a nodes <see cref="CXDiagnostic"/>s should be compared for equality between
    ///     <see cref="ICXNode"/>s.
    /// </summary>
    CompareDiagnostics = 1 << 4,

    /// <summary>
    ///     Includes all <see cref="SyntaxEqualityFlags"/>.
    /// </summary>
    All = byte.MaxValue
}