namespace ComponentDesigner.Parser;

/// <summary>
///     Represents a collection of lines within a <see cref="CXSourceText"/>.
/// </summary>
public abstract class TextLineCollection
{
    /// <summary>
    ///     Gets the number of lines.
    /// </summary>
    public abstract int Count { get; }
    
    /// <summary>
    ///     Gets the <see cref="TextLine"/> at the given index.
    /// </summary>
    /// <param name="index">The index of the line to get.</param>
    public abstract TextLine this[int index] { get; }

    /// <summary>
    ///     Gets the line index of a given offset within the <see cref="CXSourceText"/>.
    /// </summary>
    /// <param name="position">The zero-based offset to find the line of.</param>
    /// <returns>
    ///     The index of the line containing the provided position.
    /// </returns>
    public abstract int IndexOf(int position);

    /// <summary>
    ///     Gets a <see cref="TextLine"/> from a given <paramref name="position"/> relative to the start of the
    ///     <see cref="CXSourceText"/>.
    /// </summary>
    /// <param name="position">
    ///     The zero-based position relative to the start of the <see cref="CXSourceText"/> to get the
    ///     <see cref="TextLine"/> of.
    /// </param>
    /// <returns>
    ///     The <see cref="TextLine"/> at the given <paramref name="position"/>.
    /// </returns>
    public TextLine GetLineFromPosition(int position) => this[IndexOf(position)];

    public LinePosition GetLinePositon(int position)
    {
        var line = GetLineFromPosition(position);
        return new(line.LineNumber, position - line.Start);
    }

    public LinePositionSpan GetLinePositionSpan(CXTextSpan span)
        => new(GetLinePositon(span.Start), GetLinePositon(span.End));

    /// <summary>
    ///     Gets a <see cref="SourceLocation"/> from a given <paramref name="position"/> relative to the start of the
    ///     <see cref="CXSourceText"/>.
    /// </summary>
    ///<param name="position">
    ///     The zero-based position relative to the start of the <see cref="CXSourceText"/> to get the
    ///     <see cref="TextLine"/> of.
    /// </param>
    /// <returns>
    ///     The <see cref="SourceLocation"/> at the given <paramref name="position"/>.
    /// </returns>
    public SourceLocation GetSourceLocation(int position)
    {
        var line = GetLineFromPosition(position);
        return new(line.LineNumber, position - line.Start, position);
    }
    
    
}