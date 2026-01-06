using System;

namespace Discord.CX.Parser;

partial class CXSourceText
{
    /// <summary>
    ///     Represents a sub-region of text in a <see cref="CXSourceText"/>.
    /// </summary>
    internal sealed class SubText : CXSourceText
    {
        /// <inheritdoc/>
        public override char this[int position]
            => _underlyingText[position + _span.Start];

        /// <inheritdoc/>
        public override int Length => _span.Length;

        private readonly CXSourceText _underlyingText;
        private readonly CXTextSpan _span;

        /// <summary>
        ///     Constructs a new <see cref="SubText"/>.
        /// </summary>
        /// <param name="underlyingText">
        ///     The underlying <see cref="CXSourceText"/> in which this sub-text resides.
        /// </param>
        /// <param name="span">The bounds of this sub-text.</param>
        public SubText(CXSourceText underlyingText, CXTextSpan span)
        {
            _underlyingText = underlyingText;
            _span = span;
        }

        /// <summary>
        ///     Gets a span that represents a region within this <see cref="SubText"/>, normalized to the underlying
        ///     <see cref="CXSourceText"/>.
        /// </summary>
        /// <param name="span">The span to composite.</param>
        /// <returns>
        ///     A composited span, relative to this <see cref="SubText"/>s span, normalized to the underlying
        ///     <see cref="CXSourceText"/>.
        /// </returns>
        private CXTextSpan GetCompositeSpan(CXTextSpan span)
        {
            var compositeStart = Math.Min(_underlyingText.Length, _span.Start + span.Start);
            var compositeEnd = Math.Min(_underlyingText.Length, compositeStart + span.Length);

            return CXTextSpan.FromBounds(compositeStart, compositeEnd);
        }

        /// <inheritdoc/>
        protected override TextLineCollection ComputeLines() => new SubTextLineInfo(this);

        /// <inheritdoc/>
        public override CXSourceText GetSubText(CXTextSpan span)
            => new SubText(_underlyingText, GetCompositeSpan(span));

        /// <summary>
        ///     Represents line information within a <see cref="SubText"/>.
        /// </summary>
        private sealed class SubTextLineInfo : TextLineCollection
        {
            /// <inheritdoc/>
            public override int Count { get; }

            /// <inheritdoc/>
            public override TextLine this[int index]
            {
                get
                {
                    if (index < 0 || index >= Count)
                        throw new ArgumentOutOfRangeException(nameof(index));

                    if (_endsWithinSplitCRLF && index == Count - 1)
                        return new(_text, _text._span.End, _text._span.End);

                    var underlyingTextLine = _text._underlyingText.Lines[index + _startLineNumberInUnderlyingText];

                    var startInUnderlyingText = Math.Max(underlyingTextLine.Start, _text._span.Start);
                    var endInUnderlyingText = Math.Min(underlyingTextLine.EndIncludingBreaks, _text._span.End);

                    var startInSubText = startInUnderlyingText - _text._span.Start;
                    var resultLine = new TextLine(_text, startInUnderlyingText, endInUnderlyingText);

                    var shouldContainLineBreak = index != Count - 1;
                    var resultContainsLineBreak = resultLine.EndIncludingBreaks > resultLine.End;

                    if (shouldContainLineBreak != resultContainsLineBreak)
                        throw new InvalidOperationException();

                    return resultLine;
                }
            }

            private readonly SubText _text;

            private readonly int _startLineNumberInUnderlyingText;
            private readonly bool _startsWithinSplitCRLF;
            private readonly bool _endsWithinSplitCRLF;

            /// <summary>
            ///     Constructs a new <see cref="SubTextLineInfo"/>.
            /// </summary>
            /// <param name="text">The <see cref="SubText"/> this <see cref="SubTextLineInfo"/> represents.</param>
            public SubTextLineInfo(SubText text)
            {
                _text = text;

                var startLineInUnderlyingText = text._underlyingText.Lines.GetLineFromPosition(text._span.Start);
                var endLineInUnderlyingText = text._underlyingText.Lines.GetLineFromPosition(text._span.End);

                _startLineNumberInUnderlyingText = startLineInUnderlyingText.LineNumber;
                Count = endLineInUnderlyingText.LineNumber - _startLineNumberInUnderlyingText + 1;

                var underlyingSpanStart = text._span.Start;
                if (
                    underlyingSpanStart == startLineInUnderlyingText.End + 1 &&
                    underlyingSpanStart == startLineInUnderlyingText.EndIncludingBreaks - 1
                )
                {
                    _startsWithinSplitCRLF = true;
                }

                var underlyingSpanEnd = text._span.End;
                if (
                    underlyingSpanEnd == endLineInUnderlyingText.End + 1 &&
                    underlyingSpanEnd == endLineInUnderlyingText.EndIncludingBreaks - 1
                )
                {
                    _endsWithinSplitCRLF = true;
                    Count++;
                }
            }

            /// <inheritdoc/>
            public override int IndexOf(int position)
            {
                if (position < 0 && position > _text._span.Length)
                    throw new ArgumentOutOfRangeException(nameof(position));

                var underlyingPosition = position + _text._span.Start;
                var underlyingLineNumber = _text._underlyingText.Lines.IndexOf(underlyingPosition);

                if (_startsWithinSplitCRLF && position != 0)
                    underlyingLineNumber++;

                return underlyingLineNumber - _startLineNumberInUnderlyingText;
            }
        }
    }
}
