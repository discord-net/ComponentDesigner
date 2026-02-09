using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using ComponentDesigner;

namespace ComponentDesigner.Parser;

/// <summary>
///     Represents text containing the CX language.
/// </summary>
public abstract partial class CXSourceText
{
    /// <summary>
    ///     Gets the length in characters of this <see cref="CXSourceText"/>.
    /// </summary>
    public abstract int Length { get; }

    /// <summary>
    ///     Gets a single character at the given position.
    /// </summary>
    /// <param name="position">The position of the character to get.</param>
    public abstract char this[int position] { get; }

    /// <summary>
    ///     Gets a string at the given <see cref="CXTextSpan"/>.
    /// </summary>
    /// <param name="span">The span containing the starting index and length of the string to get.</param>
    public virtual string this[CXTextSpan span] => this[span.Start, span.Length];

    /// <summary>
    ///     Gets a string at the given position with the given length.
    /// </summary>
    /// <param name="position">The position of the first character of the string.</param>
    /// <param name="length">The length of the string to return.</param>
    public virtual string this[int position, int length]
    {
        get
        {
            var slice = new char[length];

            for (var i = 0; i < length; i++)
                slice[i] = this[position + i];

            return new string(slice);
        }
    }

    /// <summary>
    ///     Gets a collection representing each line within this <see cref="CXSourceText"/>.
    /// </summary>
    public TextLineCollection Lines => _lines ??= ComputeLines();

    private TextLineCollection? _lines;

    /// <summary>
    ///     Creates a new <see cref="CXSourceReader"/> from this <see cref="CXSourceText"/>.
    /// </summary>
    /// <param name="span">The relative span of this <see cref="CXSourceText"/>.</param>
    /// <param name="interpolations">
    ///     An array of <see cref="CXTextSpan"/>s indicating where in this <see cref="CXSourceText"/> interpolations lay.
    /// </param>
    /// <param name="wrappingQuoteCount">
    ///     The number of C# quotes wrapping this <see cref="CXSourceText"/>. 
    /// </param>
    /// <returns>
    ///     A <see cref="CXSourceReader"/> that can read from this <see cref="CXSourceText"/>.
    /// </returns>
    public CXSourceReader CreateReader(
        CXTextSpan[]? interpolations = null,
        int? wrappingQuoteCount = null
    ) => new(
        this,
        interpolations ?? [],
        wrappingQuoteCount ?? 3
    );

    /// <summary>
    ///     Creates a new <see cref="CXSourceText"/> representing a sub-region of this <see cref="CXSourceText"/>.
    /// </summary>
    /// <param name="span">
    ///     The <see cref="CXTextSpan"/> representing the bounds of the sub-region to get.
    /// </param>
    /// <returns>
    ///     A <see cref="CXSourceText"/> representing a sub-region of this <see cref="CXSourceText"/>.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     The provided <paramref name="span"/> was outside the bounds of this <see cref="CXSourceText"/>.
    /// </exception>
    public virtual CXSourceText GetSubText(CXTextSpan span)
    {
        if (span.Length == 0) return new StringSource(string.Empty);

        if (span.Length == Length && span.Start == 0) return this;

        if (span.End > Length) throw new ArgumentOutOfRangeException(nameof(span));

        return new SubText(this, span);
    }

    /// <summary>
    ///     Returns a new <see cref="CXSourceText"/> with the provided collection of <see cref="CXTextChange"/>s applied
    ///     to it.  
    /// </summary>
    /// <param name="changes">
    ///     The changes to apply in the new <see cref="CXSourceText"/>. 
    /// </param>
    /// <exception cref="InvalidOperationException">
    ///     Some changes overlap eachother.
    /// </exception>
    public virtual CXSourceText WithChanges(params IReadOnlyCollection<CXTextChange> changes)
    {
        if (changes.Count == 0) return this;

        var segments = new List<CXSourceText>();
        var changeRanges = new List<CXTextChangeRange>();

        var pos = 0;
        foreach (var change in changes)
        {
            if (change.Span.Start < pos)
            {
                if (change.Span.End <= changeRanges.Last().Span.Start)
                {
                    return WithChanges(
                        changes
                            .Where(x => !x.Span.IsEmpty || x.NewText?.Length > 0)
                            .OrderBy(x => x.Span)
                            .ToList()
                    );
                }

                throw new InvalidOperationException("Changes cannot overlap.");
            }

            var newTextLength = change.NewText?.Length ?? 0;

            if (change.Span.Length == 0 && newTextLength == 0)
                continue;

            if (change.Span.Start > pos)
            {
                var sub = GetSubText(new(pos, change.Span.Start - pos));
                CompositeText.AddSegments(segments, sub);
            }

            if (newTextLength > 0)
            {
                var segment = new StringSource(change.NewText!);
                CompositeText.AddSegments(segments, segment);
            }

            pos = change.Span.End;
            changeRanges.Add(new(change.Span, newTextLength));
        }

        if (pos == 0 && segments.Count == 0) return this;

        if (pos < Length)
        {
            var subText = GetSubText(new(pos, Length - pos));
            CompositeText.AddSegments(segments, subText);
        }

        var newText = new CompositeText([..segments], this);

        return new ChangedText(this, newText, [..changeRanges]);
    }

    /// <summary>
    ///     Diffs this <see cref="CXSourceText"/> with the provided <see cref="CXSourceText"/>.
    /// </summary>
    /// <param name="oldText">
    ///     The <see cref="CXSourceText"/> to diff against.
    /// </param>
    /// <returns>
    ///     A read-only collection of <see cref="CXTextChangeRange"/>s representing the changes between this
    ///     <see cref="CXSourceText"/> and the provided <see cref="CXSourceText"/>.
    /// </returns>
    public virtual IReadOnlyList<CXTextChangeRange> GetChangeRanges(CXSourceText oldText)
    {
        if (oldText == this) return [];

        return [new CXTextChangeRange(new(0, oldText.Length), Length)];
    }

    /// <summary>
    ///     Replaces text within this <see cref="CXSourceText"/> with the provided value.
    /// </summary>
    /// <param name="span">The location of the text to replace.</param>
    /// <param name="newText">The text to replace with; <see langword="null"/> to delete it.</param>
    /// <returns>
    ///     A <see cref="CXSourceText"/> with the applied replacement.
    /// </returns>
    public CXSourceText Replace(CXTextSpan span, string? newText)
        => WithChanges(new CXTextChange(span, newText ?? string.Empty));

    /// <summary>
    ///     Diffs this <see cref="CXSourceText"/> with the provided <see cref="CXSourceText"/>.
    /// </summary>
    /// <param name="oldText">
    ///     The <see cref="CXSourceText"/> to diff against.
    /// </param>
    /// <returns>
    ///     A read-only collection of <see cref="CXTextChange"/>s representing the changes between this
    ///     <see cref="CXSourceText"/> and the provided <see cref="CXSourceText"/>.
    /// </returns>
    public virtual IReadOnlyList<CXTextChange> GetTextChanges(CXSourceText oldText)
    {
        var newPosDelta = 0;

        var ranges = GetChangeRanges(oldText);
        var results = new List<CXTextChange>();

        foreach (var range in ranges)
        {
            var newPos = range.Span.Start + newPosDelta;

            var text = range.NewLength > 0
                ? this[new CXTextSpan(newPos, range.NewLength)].ToString()
                : string.Empty;

            results.Add(new(range.Span, text));
            newPosDelta += range.NewLength - range.Span.Length;
        }

        return results;
    }

    /// <summary>
    ///     Computes the lines within this <see cref="CXSourceText"/>.
    /// </summary>
    /// <returns>
    ///     A <see cref="TextLineCollection"/> representing the lines within this <see cref="CXSourceText"/>.
    /// </returns>
    protected virtual TextLineCollection ComputeLines()
        => new LineInfo(this, ParseLineOffsets());

    /// <summary>
    ///     Parses the lines within this <see cref="CXSourceText"/> and returns them as an array of offsets indicating
    ///     the position of when the line starts.
    /// </summary>
    private ImmutableArray<int> ParseLineOffsets()
    {
        if (Length == 0) return [0];

        var lineStarts = new List<int>(Length / 64) { 0 };

        for (var i = 0; i < Length; i++)
        {
            var ch = this[i];

            const uint bias = '\r' + 1;
            if (unchecked(ch - bias) <= 127 - bias)
                continue;

            if (ch is '\r')
            {
                if (Length == i + 1)
                    break;

                if (this[i + 1] is '\n')
                {
                    i += 2;
                    lineStarts.Add(i);
                    continue;
                }

                lineStarts.Add(i + 1);
                continue;
            }

            if (!ch.IsNewline()) continue;

            lineStarts.Add(i + 1);
        }

        return [..lineStarts];
    }

    /// <summary>
    ///     Gets the string value of this <see cref="CXSourceText"/>.
    /// </summary>
    public override string ToString()
        => this[new CXTextSpan(0, Length)];

    /// <summary>
    ///     Represents information about the lines within this <see cref="CXSourceText"/>.
    /// </summary>
    private sealed class LineInfo : TextLineCollection
    {
        /// <inheritdoc/>
        public override int Count => _lineOffsets.Length;

        /// <inheritdoc/>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     The index was out of bounds.
        /// </exception>
        public override TextLine this[int index]
        {
            get
            {
                if (index < 0 || index >= _lineOffsets.Length)
                    throw new ArgumentOutOfRangeException(nameof(index));

                var start = _lineOffsets[index];

                var end = index == _lineOffsets.Length - 1
                    ? _source.Length
                    : _lineOffsets[index + 1];

                return new(_source, start, end);
            }
        }

        private readonly CXSourceText _source;
        private readonly ImmutableArray<int> _lineOffsets;

        /// <summary>
        ///     Constructs a new <see cref="LineInfo"/>.
        /// </summary>
        /// <param name="source">
        ///     The <see cref="CXSourceText"/> this line info is within.
        /// </param>
        /// <param name="lineOffsets">
        ///     The offsets of each line within the <see cref="CXSourceText"/>.
        /// </param>
        public LineInfo(CXSourceText source, ImmutableArray<int> lineOffsets)
        {
            _source = source;
            _lineOffsets = lineOffsets;
        }

        /// <inheritdoc/>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     The provided position is out of bounds.
        /// </exception>
        public override int IndexOf(int position)
        {
            if (position < 0 || position > _source.Length)
                throw new ArgumentOutOfRangeException(nameof(position));

            var lineNumber = _lineOffsets.BinarySearch(position);

            if (lineNumber < 0) lineNumber = ~lineNumber - 1;

            return lineNumber;
        }
    }
}