using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Discord.CX.Parser;

partial class CXSourceText
{
    /// <summary>
    ///     Represents a sequence of <see cref="CXSourceText"/>.
    /// </summary>
    internal sealed class CompositeText : CXSourceText
    {
        /// <inheritdoc/>
        public override char this[int position]
        {
            get
            {
                GetIndexAndOffset(position, out var index, out var offset);
                return _segments[index][offset];
            }
        }
        
        /// <inheritdoc/>
        public override int Length { get; }

        private readonly ImmutableArray<CXSourceText> _segments;
        private readonly CXSourceText _original;
        private readonly int[] _offsets;

        /// <summary>
        ///     Constructs a new <see cref="CompositeText"/>.
        /// </summary>
        /// <param name="segments">The segments making up this <see cref="CompositeText"/>.</param>
        /// <param name="original">The original <see cref="CXSourceText"/> that created this composite text.</param>
        public CompositeText(ImmutableArray<CXSourceText> segments, CXSourceText original)
        {
            _segments = segments;
            _original = original;

            _offsets = new int[segments.Length];

            for (var i = 0; i < segments.Length; i++)
            {
                _offsets[i] = Length;
                Length += segments[i].Length;
            }
        }

        /// <summary>
        ///     Computes the segment index and offset of a given position.
        /// </summary>
        /// <param name="pos">The position to compute the index and offset from.</param>
        /// <param name="index">The index of the segment containing the given position.</param>
        /// <param name="offset">The offset within the segment representing the given position.</param>
        private void GetIndexAndOffset(int pos, out int index, out int offset)
        {
            var idx = BinSearchOffsets(pos);
            index = idx >= 0 ? idx : (~idx - 1);
            offset = pos - _offsets[index];
        }

        /// <summary>
        ///     Searches for a position within the segments using a binary search.
        /// </summary>
        /// <param name="pos">The position to search for.</param>
        /// <returns>The index to the segment containing the position.</returns>
        private int BinSearchOffsets(int pos)
        {
            var low = 0;
            var high = _offsets.Length - 1;

            while (low <= high)
            {
                var mid = low + ((high - low) >> 1);
                var midVal = _offsets[mid];

                if (midVal == pos) return mid;

                if (midVal > pos)
                {
                    high = mid - 1;
                    continue;
                }

                low = mid + 1;
            }

            return ~low;
        }

        /// <summary>
        ///     Adds a given <see cref="CXSourceText"/> to a given collection of segments. 
        /// </summary>
        /// <param name="segments">The collection to add the segment to.</param>
        /// <param name="text">The <see cref="CXSourceText"/> to add.</param>
        public static void AddSegments(List<CXSourceText> segments, CXSourceText text)
        {
            if (text is CompositeText composite)
                segments.AddRange(composite._segments);
            else segments.Add(text);
        }

        /// <summary>
        ///     Represents line information within a <see cref="CompositeText"/>.
        /// </summary>
        private sealed class CompositeTextLineInfo : TextLineCollection
        {
            /// <inheritdoc/>
            public override int Count => _lineCount;

            /// <inheritdoc/>
            public override TextLine this[int index]
            {
                get
                {
                    if (index < 0 || index >= _lineCount)
                        throw new ArgumentOutOfRangeException(nameof(index));

                    GetSegmentIndexRangeContainingLine(
                        index,
                        out var firstSegmentIndexInclusive,
                        out var lastSegmentIndexInclusive
                    );

                    var firstSegmentFirstLineNumber = _segmentLineNumbers[firstSegmentIndexInclusive];
                    var firstSegment = _text._segments[firstSegmentIndexInclusive];
                    var firstSegmentOffset = _text._offsets[firstSegmentIndexInclusive];
                    var firstSegmentTextLine = firstSegment.Lines[index - firstSegmentFirstLineNumber];

                    var lineLength = firstSegmentTextLine.SpanIncludingBreaks.Length;

                    for (
                        var nextSegmentIndex = firstSegmentIndexInclusive + 1;
                        nextSegmentIndex < lastSegmentIndexInclusive;
                        nextSegmentIndex++
                    )
                    {
                        var nextSegment = _text._segments[nextSegmentIndex];

                        lineLength += nextSegment.Lines[0].SpanIncludingBreaks.Length;
                    }

                    if (firstSegmentIndexInclusive != lastSegmentIndexInclusive)
                    {
                        var lastSegment = _text._segments[lastSegmentIndexInclusive];
                        lineLength += lastSegment.Lines[0].SpanIncludingBreaks.Length;
                    }

                    return new TextLine(
                        _text,
                        firstSegmentOffset + firstSegmentTextLine.Start,
                        firstSegmentOffset + firstSegmentTextLine.Start + lineLength
                    );
                }
            }

            private readonly CompositeText _text;
            private readonly ImmutableArray<int> _segmentLineNumbers;
            private readonly int _lineCount;

            /// <summary>
            ///     Constructs a new <see cref="CompositeTextLineInfo"/>.
            /// </summary>
            /// <param name="text">The <see cref="CompositeText"/> to represent.</param>
            public CompositeTextLineInfo(CompositeText text)
            {
                var segmentLineNumbers = new int[text._segments.Length];
                var accumulatedLineCount = 0;

                for (var i = 0; i < text._segments.Length; i++)
                {
                    segmentLineNumbers[i] = accumulatedLineCount;

                    var segment = text._segments[i];
                    accumulatedLineCount += segment.Lines.Count;
                }

                _segmentLineNumbers = [..segmentLineNumbers];
                _text = text;
                _lineCount = accumulatedLineCount + 1;
            }

            /// <inheritdoc/>
            public override int IndexOf(int position)
            {
                if (position < 0 || position >= _text.Length)
                    throw new ArgumentOutOfRangeException(nameof(position));

                _text.GetIndexAndOffset(position, out var index, out var offset);

                var segment = _text._segments[index];
                var lineNumberWithinSegment = segment.Lines.IndexOf(offset);

                return _segmentLineNumbers[index] + lineNumberWithinSegment;
            }

            /// <summary>
            ///     Computes the inclusive range of segment indexes containing a given line.
            /// </summary>
            /// <param name="lineNumber">The line number to find the segment indexes of.</param>
            /// <param name="firstSegmentIndexInclusive">The first segments inclusive index containing the line.</param>
            /// <param name="lastSegmentIndexInclusive">The second segments inclusive index containing the line.</param>
            private void GetSegmentIndexRangeContainingLine(
                int lineNumber,
                out int firstSegmentIndexInclusive,
                out int lastSegmentIndexInclusive
            )
            {
                var idx = _segmentLineNumbers.BinarySearch(lineNumber);
                var binarySearchSegmentIndex = idx >= 0 ? idx : (~idx - 1);

                for (
                    firstSegmentIndexInclusive = binarySearchSegmentIndex;
                    firstSegmentIndexInclusive > 0;
                    firstSegmentIndexInclusive--
                )
                {
                    if (_segmentLineNumbers[firstSegmentIndexInclusive] != lineNumber)
                        break;

                    var previousSegment = _text._segments[firstSegmentIndexInclusive - 1];
                    var previousSegmentLastChar = previousSegment[previousSegment.Length - 1];

                    if (previousSegmentLastChar.IsNewline()) break;
                }

                for (
                    lastSegmentIndexInclusive = binarySearchSegmentIndex;
                    lastSegmentIndexInclusive < _text._segments.Length - 1;
                    lastSegmentIndexInclusive++
                )
                {
                    if (_segmentLineNumbers[lastSegmentIndexInclusive + 1] != lineNumber)
                        break;
                }
            }
        }
    }
}
