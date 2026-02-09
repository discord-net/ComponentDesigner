using System.Collections.Immutable;

namespace ComponentDesigner.Parser;

/// <summary>
///     A data type representing the position within a syntax tree, source file, and the difference between them. 
/// </summary>
/// <param name="NodeIndex">The index into the AST tree this cursor points to.</param>
/// <param name="ChangeDelta">
///     The difference between the source and the AST tree, expressed as a delta.
/// </param>
/// <param name="NewPosition">The new source position.</param>
/// <param name="Changes">A stack containing the upcoming changes between the AST and source.</param>
public readonly record struct Cursor(
    int NodeIndex,
    int ChangeDelta,
    int NewPosition,
    ImmutableStack<CXTextChangeRange> Changes
)
{
    /// <summary>
    ///     Creates a new cursor with the index pointing to an invalid node.
    /// </summary>
    public Cursor Finish()
        => this with { NodeIndex = -1 };
}