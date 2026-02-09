namespace ComponentDesigner.Parser;

/// <summary>
///     Represents a location within a <see cref="CXSourceText"/>.
/// </summary>
/// <param name="Line">The zero-based line number of the location.</param>
/// <param name="Column">The zero-based column number of the location.</param>
/// <param name="Position">
///     The zero-based position of the location, from the start of the <see cref="CXSourceText"/>.
/// </param>
public readonly record struct SourceLocation(
    int Line,
    int Column,
    int Position
);
