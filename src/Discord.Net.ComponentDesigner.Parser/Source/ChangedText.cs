using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Discord.CX.Parser;

partial class CXSourceText
{
    /// <summary>
    ///     Represents a region of changed text within a <see cref="CXSourceText"/>.
    /// </summary>
    internal sealed class ChangedText : CXSourceText
    {
        /// <summary>
        ///     Represents the change information about a change.
        /// </summary>
        /// <param name="Changes">The <see cref="CXTextChangeRange"/>s representing the changes.</param>
        /// <param name="WeakOldText">
        ///     A weak reference to the old <see cref="CXSourceText"/> before the changes were applied.
        /// </param>
        /// <param name="Previous">The previous change information applied before this one.</param>
        private sealed record ChangeInfo(
            ImmutableArray<CXTextChangeRange> Changes,
            WeakReference<CXSourceText> WeakOldText,
            ChangeInfo? Previous = null
        )
        {
            /// <summary>
            ///     Gets the previous change information, if any.
            /// </summary>
            public ChangeInfo? Previous { get; private set; } = Previous;

            /// <summary>
            ///     Cleans this and any previous changes by merging them into this one.
            /// </summary>
            public void Clean()
            {
                var lastInfo = this;
                for (var info = this; info is not null; info = info.Previous)
                {
                    if (info.WeakOldText.TryGetTarget(out _))
                        lastInfo = info;
                }

                while (lastInfo is not null)
                {
                    var prev = lastInfo.Previous;
                    lastInfo.Previous = null;
                    lastInfo = prev;
                }
            }
        }

        /// <inheritdoc/>
        public override char this[int position] => _newText[position];

        /// <inheritdoc/>
        public override int Length => _newText.Length;

        private readonly CXSourceText _newText;
        private readonly ChangeInfo _info;

        /// <summary>
        ///     Constructs a new <see cref="ChangedText"/>.
        /// </summary>
        /// <param name="oldText">The old <see cref="CXSourceText"/> before the changes.</param>
        /// <param name="newText">The new <see cref="CXSourceText"/> after the changes.</param>
        /// <param name="changes">The changes that were made.</param>
        public ChangedText(
            CXSourceText oldText,
            CXSourceText newText,
            ImmutableArray<CXTextChangeRange> changes
        )
        {
            _newText = newText;
            _info = new(changes, new(oldText), (oldText as ChangedText)?._info);
        }

        /// <inheritdoc/>
        protected override TextLineCollection ComputeLines() => _newText.Lines;

        /// <inheritdoc/>
        public override CXSourceText WithChanges(params IReadOnlyCollection<CXTextChange> changes)
        {
            var changed = _newText.WithChanges(changes);

            if (changed is ChangedText changedText)
                return new ChangedText(this, changedText._newText, changedText._info.Changes);

            return changed;
        }

        /// <inheritdoc/>
        public override IReadOnlyList<CXTextChangeRange> GetChangeRanges(CXSourceText oldText)
        {
            if (_info.WeakOldText.TryGetTarget(out var actualOldText) && actualOldText == oldText)
                return _info.Changes;

            if (IsChangedFrom(oldText))
            {
                var changes = GetChangesBetween(oldText, this);

                if (changes.Count > 1) return Merge(changes);
            }

            if (actualOldText is not null && actualOldText.GetChangeRanges(oldText).Count == 0)
                return _info.Changes;

            return [new CXTextChangeRange(new(0, oldText.Length), _newText.Length)];
        }

        /// <summary>
        ///     Determines if the this <see cref="ChangedText"/> was changed from the provided
        ///     <see cref="CXSourceText"/>.
        /// </summary>
        /// <param name="oldText">The <see cref="CXSourceText"/> to check against.</param>
        /// <returns>
        ///     <see langword="true"/> if the provided <see cref="CXSourceText"/> was changed by this
        ///     <see cref="ChangedText"/>; otherwise <see langword="false"/>.
        /// </returns>
        private bool IsChangedFrom(CXSourceText oldText)
        {
            for (var info = _info; info is not null; info = info.Previous)
            {
                if (info.WeakOldText.TryGetTarget(out var text) && text == oldText)
                    return true;
            }

            return false;
        }

        /// <summary>
        ///     Computes the changes between a given <see cref="CXSourceText"/> and a <see cref="ChangedText"/>.
        /// </summary>
        /// <param name="oldText">The <see cref="CXSourceText"/> to start getting changes from.</param>
        /// <param name="newText">The <see cref="ChangedText"/> buck to stop at.</param>
        /// <returns>
        ///     A 2d collection of <see cref="CXTextChangeRange"/> describing each step of changes made between
        ///     the <paramref name="oldText"/> and <paramref name="newText"/>
        /// </returns>
        private static IReadOnlyList<ImmutableArray<CXTextChangeRange>> GetChangesBetween(
            CXSourceText oldText,
            ChangedText newText
        )
        {
            var results = new List<ImmutableArray<CXTextChangeRange>>();

            var info = newText._info;
            results.Add(info.Changes);

            while (info is not null)
            {
                info.WeakOldText.TryGetTarget(out var actualOldText);

                if (actualOldText == oldText) return results;

                if ((info = info.Previous) is not null)
                    results.Insert(0, info.Changes);
            }

            results.Clear();
            return results;
        }

        /// <summary>
        ///     Merges a 2d collection of changes representing the change history into a single collection of changes.
        /// </summary>
        /// <param name="changes">The changes to merge.</param>
        /// <returns>A merged collection of changes.</returns>
        private static ImmutableArray<CXTextChangeRange> Merge(IReadOnlyList<ImmutableArray<CXTextChangeRange>> changes)
        {
            var merged = changes[0];
            for (var i = 1; i < changes.Count; i++)
            {
                merged = Merge(merged, changes[i]);
            }

            return merged;
        }

        /// <summary>
        ///     Merges 2 sets of changes representing part of a history into a single collection.
        /// </summary>
        /// <param name="oldChanges">The old changes to merge.</param>
        /// <param name="newChanges">The new changes to merge with.</param>
        /// <returns>The merged changes as a read-only array of <see cref="CXTextChangeRange"/>.</returns>
        /// <exception cref="InvalidOperationException" />
        /// <exception cref="ArgumentOutOfRangeException">Change exceeds the source.</exception>
        private static ImmutableArray<CXTextChangeRange> Merge(
            ImmutableArray<CXTextChangeRange> oldChanges,
            ImmutableArray<CXTextChangeRange> newChanges
        )
        {
            var results = new List<CXTextChangeRange>();

            var oldChange = oldChanges[0];
            var newChange = new UnadjustedNewChange(newChanges[0]);

            var oldIndex = 0;
            var newIndex = 0;

            var oldDelta = 0;

            while (true)
            {
                if (oldChange is { Span.Length: 0, NewLength: 0 })
                {
                    // old change doesn't insert or delete anything, so it can be discarded.
                    if (TryGetNextOldChange()) continue;

                    break;
                }

                if (newChange is { SpanLength: 0, NewLength: 0 })
                {
                    // new change doesn't insert or delete anything, so it can be discarded.
                    if (TryGetNextNewChange()) continue;
                    break;
                }

                if (newChange.SpanEnd <= oldChange.Span.Start + oldDelta)
                {
                    // new change is before old change, so just take the new change
                    AdjustAndAddNewChange(results, oldDelta, newChange);

                    if (TryGetNextNewChange()) continue;

                    break;
                }

                if (newChange.SpanStart >= oldChange.Span.Start + oldChange.NewLength + oldDelta)
                {
                    // new change is after old change, so just take the old change
                    AddAndAdjustOldDelta(results, ref oldDelta, oldChange);

                    if (TryGetNextOldChange()) continue;
                    break;
                }

                if (newChange.SpanStart < oldChange.Span.Start + oldDelta)
                {
                    // new change overlaps
                    var newChangeLeadingDeletion = oldChange.Span.Start + oldDelta - newChange.SpanStart;
                    AdjustAndAddNewChange(
                        results,
                        oldDelta,
                        new(
                            newChange.SpanStart,
                            newChangeLeadingDeletion,
                            NewLength: 0
                        )
                    );
                    newChange = newChange with
                    {
                        SpanStart = oldChange.Span.Start + oldDelta,
                        SpanLength = newChange.SpanLength - newChangeLeadingDeletion,
                    };
                    continue;
                }

                if (newChange.SpanStart > oldChange.Span.Start + oldDelta)
                {
                    // new change starts after old change, but it overlaps

                    var oldChangeLeadingInsertion = newChange.SpanStart - (oldChange.Span.Start + oldDelta);
                    var oldChangeLeadingDeletion = Math.Min(oldChange.Span.Length, oldChangeLeadingInsertion);
                    AddAndAdjustOldDelta(
                        results,
                        ref oldDelta,
                        new CXTextChangeRange(
                            CXTextSpan.FromBounds(oldChange.Span.Start, oldChangeLeadingDeletion),
                            oldChangeLeadingInsertion
                        )
                    );

                    oldChange = new CXTextChangeRange(
                        new CXTextSpan(newChange.SpanStart - oldDelta, oldChange.Span.Length - oldChangeLeadingDeletion),
                        oldChange.NewLength - oldChangeLeadingInsertion
                    );
                    continue;
                }

                // old and new change start at the same position
                if (newChange.SpanLength <= oldChange.NewLength)
                {
                    // new change deletes less
                    oldChange = new(oldChange.Span, oldChange.NewLength - newChange.SpanLength);

                    oldDelta += newChange.SpanLength;
                    newChange = newChange with { SpanLength = 0 };
                    AdjustAndAddNewChange(results, oldDelta, newChange);

                    if (TryGetNextNewChange()) continue;
                    break;
                }

                // new change deletes more
                oldDelta -= oldChange.Span.Length + oldChange.NewLength;

                var newDeletion = newChange.SpanLength + oldChange.Span.Length - oldChange.NewLength;
                newChange = newChange with { SpanStart = oldChange.Span.Start + oldDelta, SpanLength = newDeletion, };

                if (TryGetNextOldChange()) continue;
                break;
            }

            // there may be remaining old changes, but they're mutually exclusive
            switch (oldIndex == oldChanges.Length, newIndex == newChanges.Length)
            {
                case (true, true) or (false, false):
                    throw new InvalidOperationException();
            }

            while (oldIndex < oldChanges.Length)
            {
                AddAndAdjustOldDelta(results, ref oldDelta, oldChange);
                TryGetNextOldChange();
            }

            while (newIndex < newChanges.Length)
            {
                AdjustAndAddNewChange(results, oldDelta, newChange);
                TryGetNextNewChange();
            }

            return [..results];

            static void AddAndAdjustOldDelta(
                List<CXTextChangeRange> results,
                ref int oldDelta,
                CXTextChangeRange oldChange
            )
            {
                oldDelta -= (oldChange.Span.Length + oldChange.NewLength);
                Add(results, oldChange);
            }

            static void AdjustAndAddNewChange(
                List<CXTextChangeRange> results,
                int oldDelta,
                UnadjustedNewChange newChange
            )
            {
                Add(
                    results,
                    new(
                        new(newChange.SpanStart - oldDelta, newChange.SpanLength),
                        newChange.NewLength
                    )
                );
            }

            static void Add(
                List<CXTextChangeRange> results,
                CXTextChangeRange change
            )
            {
                if (results.Count == 0)
                {
                    results.Add(change);
                    return;
                }

                var last = results[results.Count - 1];
                if (last.Span.End == change.Span.Start)
                {
                    // merge
                    results[results.Count - 1] = new(
                        new CXTextSpan(last.Span.Start, last.Span.Length + change.Span.Length),
                        last.NewLength + change.NewLength
                    );
                    return;
                }

                if (last.Span.End > change.Span.Start)
                {
                    throw new ArgumentOutOfRangeException(nameof(change));
                }

                results.Add(change);
            }


            bool TryGetNextNewChange()
            {
                newIndex++;
                if (newIndex < newChanges.Length)
                {
                    newChange = new UnadjustedNewChange(newChanges[newIndex]);
                    return true;
                }

                newChange = default;
                return false;
            }

            bool TryGetNextOldChange()
            {
                oldIndex++;
                if (oldIndex < oldChanges.Length)
                {
                    oldChange = oldChanges[oldIndex];
                    return true;
                }

                oldChange = default;
                return false;
            }
        }

        /// <summary>
        ///     Represents an unadjusted new change.
        /// </summary>
        /// <param name="SpanStart">The start of the change.</param>
        /// <param name="SpanLength">The length of the change.</param>
        /// <param name="NewLength">The new length after the change.</param>
        private readonly record struct UnadjustedNewChange(
            int SpanStart,
            int SpanLength,
            int NewLength
        )
        {
            /// <summary>
            ///     Gets the ending point of the changed span.
            /// </summary>
            public int SpanEnd => SpanStart + SpanLength;

            /// <summary>
            ///     Constructs a new <see cref="UnadjustedNewChange"/>.
            /// </summary>
            /// <param name="range">
            ///     The <see cref="CXTextChangeRange"/> containing the information to constuct this
            ///     <see cref="UnadjustedNewChange"/> with.
            /// </param>
            public UnadjustedNewChange(CXTextChangeRange range) : this(range.Span.Start, range.Span.Length,
                range.NewLength)
            {
            }
        }
    }
}