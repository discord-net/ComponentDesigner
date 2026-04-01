using ComponentDesigner;

namespace ComponentDesigner.Parser;

/// <summary>
///     Represents a single line within a <see cref="CXSourceText"/>.
/// </summary>
/// <param name="Source">The <see cref="CXSourceText"/> containing this line.</param>
/// <param name="Start">The zero-based offset of the <see cref="CXSourceText"/> that this line starts at.</param>
/// <param name="EndIncludingBreaks">
///     The zero-based offset of the <see cref="CXSourceText"/> that this line ends at.
/// </param>
public readonly record struct TextLine(
    CXSourceText Source,
    int Start,
    int EndIncludingBreaks
)
{
    public int Length => Span.Length;
    
    /// <summary>
    ///     Gets the zero-based line number of this <see cref="TextLine"/>.
    /// </summary>
    public int LineNumber => Source.Lines.IndexOf(Start);

    /// <summary>
    ///     Gets the zero-based end position of this line, excluding line breaks
    /// </summary>
    public int End => EndIncludingBreaks - LineBreakLength;

    /// <summary>
    ///     Gets a <see cref="CXTextSpan"/> representing this line, excluding line breaks.
    /// </summary>
    public CXTextSpan Span => CXTextSpan.FromBounds(Start, End);
    
    /// <summary>
    ///     Gets a <see cref="CXTextSpan"/> representing this line, including line breaks.
    /// </summary>
    public CXTextSpan SpanIncludingBreaks => CXTextSpan.FromBounds(Start, End);

    /// <summary>
    ///     Gets the length in characters of this <see cref="TextLine"/>s linebreak.
    /// </summary>
    private int LineBreakLength
    {
        get
        {
            var ch = Source[EndIncludingBreaks - 1];

            if (ch is '\n')
            {
                if (EndIncludingBreaks > 1 && Source[EndIncludingBreaks - 2] is '\r')
                    return 2;

                return 1;
            }

            if (ch.IsNewline()) return 1;

            return 0;
        }
    }
}