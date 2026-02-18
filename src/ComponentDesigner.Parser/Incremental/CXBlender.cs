using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace ComponentDesigner.Parser;

/// <summary>
///     Represents a way to mix (blend) between an old AST and new source, providing the result as a
///     <see cref="BlendedNode"/>.
/// </summary>
public sealed class CXBlender
{
    /// <summary>
    ///     A cursor representing the starting point of the AST and source.
    /// </summary>
    public readonly Cursor StartingCursor;

    /// <summary>
    ///     Gets the cancellation token used to cancel the blending operation.
    /// </summary>
    private CancellationToken CancellationToken => _lexer.CancellationToken;

    private readonly IReadOnlyList<ICXNode> _graph;
    private readonly CXLexer _lexer;

    /// <summary>
    ///     Constructs a new <see cref="CXBlender"/>.
    /// </summary>
    /// <param name="lexer">The lexer used to lex new tokens.</param>
    /// <param name="document">The old AST to blend from.</param>
    /// <param name="changeRange">
    ///     A range describing the change between the <paramref name="document"/> and <paramref name="lexer"/>.
    /// </param>
    public CXBlender(
        CXLexer lexer,
        CXDocument document,
        CXTextChangeRange changeRange
    )
    {
        _lexer = lexer;
        _graph = document.GetFlatGraph();

        StartingCursor = new(
            0,
            0,
            0,
            ImmutableStack<CXTextChangeRange>
                .Empty
                .Push(changeRange)
        );
    }

    /// <summary>
    ///     Checks if a cursor is complete, meaning there is no further blending to be done with it.
    /// </summary>
    /// <param name="cursor">The cursor to check.</param>
    /// <returns>
    ///     <see langword="true"/> if there is no more blending to be done with the given cursor; otherwise
    ///     <see langword="false"/>.
    /// </returns>
    private bool IsComplete(Cursor cursor)
        => cursor.NodeIndex < 0 || cursor.NodeIndex >= _graph.Count;

    /// <summary>
    ///     Gets the <see cref="ICXNode"/> that the given cursor represents.
    /// </summary>
    /// <param name="cursor">
    ///     The cursor to use to get the AST node of.
    /// </param>
    /// <remarks>
    ///     If the cursor is determined to be complete (using <see cref="IsComplete"/>), <see langword="null"/> is
    ///     returned.
    /// </remarks>
    private ICXNode? this[Cursor cursor]
        => IsComplete(cursor) ? null : _graph[cursor.NodeIndex];

    /// <summary>
    ///     Blends between the AST tree and changes using the given <see cref="Cursor"/>. 
    /// </summary>
    /// <param name="asToken">Whether to blend a token only.</param>
    /// <param name="cursor">The cursor representing where to blend from.</param>
    /// <returns>
    ///     A <see cref="BlendedNode"/> representing the AST node blended OR the <see cref="CXToken"/> lexed.
    /// </returns>
    public BlendedNode Next(bool asToken, Cursor cursor)
    {
        while (true)
        {
            CancellationToken.ThrowIfCancellationRequested();

            /*
             * If the cursor is complete, the only thing we can do is lex from the new source.
             *
             * If we've passed the end of the source, the lexer should return a EOF token for us.
             */
            if (IsComplete(cursor))
            {
                var token = LexNew(ref cursor);
                return new(cursor, token, false);
            }

            /*
             * If we have a non-zero change delta that means the source differs from the AST tree, either:
             *
             *   - we're further ahead in the new source (change delta is negative) and we should skip
             *     past tokens in the old AST tree until we catch up to the source
             *
             *   - we're further ahead in the old AST tree (change delta is positive) and we should lex
             *     the new source until we're caught up to the AST tree
             */
            if (cursor.ChangeDelta < 0)
            {
                // we can skip old nodes/tokens
                SkipOldNode(ref cursor);
            }
            else if (cursor.ChangeDelta > 0)
            {
                // we're behind in the source, so read a new token
                var token = LexNew(ref cursor);
                return new(cursor, token, false);
            }
            else
            {
                // attempt to reuse an old node/token and return that, in the case that we can't, we traverse down the
                // AST to the child and try again
                if (TryTakeOldNode(asToken, ref cursor, out var node))
                    return new(cursor, node, true);

                // move to the first child if it's a non-terminal AST node 
                if (this[cursor] is CXNode) MoveToFirstChild(ref cursor);

                // otherwise we skip past the token
                else SkipOldToken(ref cursor);
            }
        }
    }

    /// <summary>
    ///     Attempts to take a node out of the old AST tree at the given cursors location and reuse it
    /// </summary>
    /// <param name="asToken">Whether the node to take should be a token.</param>
    /// <param name="cursor">The cursor describing where in the AST tree to take the node from.</param>
    /// <param name="node">The AST node that was reused.</param>
    /// <returns>
    ///     <see langword="true"/> if an AST node could be reused; otherwise <see langword="false"/>.
    /// </returns>
    private bool TryTakeOldNode(bool asToken, ref Cursor cursor, [MaybeNullWhen(false)] out ICXNode node)
    {
        // ensure we're looking at a token if we're specified to
        if (asToken) MoveToFirstToken(ref cursor);

        // do we actually point to something in the AST tree?
        if (IsComplete(cursor))
        {
            node = null;
            return false;
        }

        var current = this[cursor];

        // check if we can reuse what the cursor points at
        if (current is null || !CanReuse(current, cursor))
        {
            node = null;
            return false;
        }

        // we can reuse it, update the cursor to point to the next node
        MoveToNextSibling(ref cursor);

        // also update the source position of the cursor to account for the width of the node 
        // we just took
        cursor = cursor with
        {
            NewPosition = cursor.NewPosition + current.FullTextSpan.Length
        };

        node = current;
        return true;
    }

    /// <summary>
    ///     Moves the given cursor to point to the first child AST node.
    /// </summary>
    /// <param name="cursor">The cursor to update.</param>
    /// <remarks>
    ///     If the AST node the cursor points to has no children, the cursor is updated to a completed cursor.
    /// </remarks>
    private void MoveToFirstChild(ref Cursor cursor)
    {
        if (this[cursor] is not { } current || current.Slots.Count is 0)
        {
            cursor = cursor.Finish();
            return;
        }

        cursor = cursor with
        {
            // the node ahead of this node will always be the first child, assuming it has children
            NodeIndex = cursor.NodeIndex + 1
        };

        Debug.Assert(
            IsComplete(cursor) || ReferenceEquals(_graph[cursor.NodeIndex], current.Slots[0]),
            "Moving to first graph child"
        );
    }

    /// <summary>
    ///     Moves the given cursor to point to the next terminal token.
    /// </summary>
    /// <param name="cursor">The cursor to move.</param>
    private void MoveToFirstToken(ref Cursor cursor)
    {
        if (this[cursor] is not { } current)
            return;

        var index = cursor.NodeIndex;
        
        while (
            index < _graph.Count &&
            _graph[index] is not CXToken
        ) index++;
        
        cursor = cursor with
        {
            NodeIndex = index
        };
    }

    /// <summary>
    ///     Moves the given cursor to point to the next sibling AST node.
    /// </summary>
    /// <param name="cursor">The cursor to move</param>
    private void MoveToNextSibling(ref Cursor cursor)
    {
        if (this[cursor] is not { } current) return;

        if (current.Parent is null)
        {
            cursor = cursor.Finish();
            return;
        }

        var index = cursor.NodeIndex;

        // add the width of this node to the index, skipping over it plus its children
        index += current.GraphWidth + 1;

        // the next graph node should be the right-most sibling of either the 'current' node or the next ancestral
        // node with a sibling of our ancestors
        cursor = cursor with
        {
            NodeIndex = index
        };

        return;
    }

    /// <summary>
    ///     Determines if an AST node can be reused.
    /// </summary>
    /// <param name="node">The node to check.</param>
    /// <param name="cursor">The cursor containing the positioning information.</param>
    /// <returns>
    ///     <see langword="true"/> if the given node can be reused; otherwise <see langword="false"/>.
    /// </returns>
    private bool CanReuse(ICXNode? node, Cursor cursor)
    {
        if (node is null) return false;

        // zero-width nodes don't get reused
        if (node.FullTextSpan.IsEmpty) return false;

        // don't reuse the node if it intersects a change
        if (IntersectsChange(node, cursor)) return false;

        // nodes with diagnostics are never reused, some diagnostics may depend on factors external to the node 
        if (node.HasDiagnostics) return false;

        // the node can be reused
        return true;
    }

    /// <summary>
    ///     Determines if the given node intersects a change within the cursor.
    /// </summary>
    /// <param name="node">The node to check.</param>
    /// <param name="cursor">The cursor containing the change information.</param>
    /// <returns>
    ///     <see langword="true"/> if the given node intersects a change; otherwise <see langword="false"/>.
    /// </returns>
    private bool IntersectsChange(ICXNode node, Cursor cursor)
    {
        if (cursor.Changes.IsEmpty) return false;
        
        return node.FullTextSpan.IntersectsWith(cursor.Changes.Peek().Span);
    }

    /// <summary>
    ///     Lexes a new token from the underlying <see cref="_lexer"/> in the source at the given
    ///     <paramref name="cursor"/>s location.
    /// </summary>
    /// <param name="cursor">The cursor describing where to lex the token.</param>
    /// <returns>The lexed token.</returns>
    private CXToken LexNew(ref Cursor cursor)
    {
        // make sure we're looking at the position in the cursor.
        _lexer.Seek(cursor.NewPosition);
        
        var token = _lexer.Next();

        // update the cursor with the tokens full width.
        cursor = cursor with
        {
            NewPosition = cursor.NewPosition + token.FullTextSpan.Length,
            ChangeDelta = cursor.ChangeDelta - token.FullTextSpan.Length
        };
        
        // skip past any changes in the cursor that we've read over.
        SkipPastChanges(ref cursor);

        return token;
    }

    /// <summary>
    ///     Skips over an old token within the AST tree at the given <paramref name="cursor"/>s location. 
    /// </summary>
    /// <param name="cursor">The cursor pointing to the AST node to skip over a token from.</param>
    private void SkipOldToken(ref Cursor cursor)
    {
        // make sure we're looking at a token
        MoveToFirstToken(ref cursor);

        if (this[cursor] is not { } current)
        {
            cursor = cursor.Finish();
            return;
        }

        // update the cursors change delta (not position) based on the tokens span
        cursor = cursor with
        {
            ChangeDelta = cursor.ChangeDelta + current.FullTextSpan.Length
        };

        // point the cursor to the next sibling and skip past any changes we've read over
        MoveToNextSibling(ref cursor);
        SkipPastChanges(ref cursor);
    }

    /// <summary>
    ///     Skips over an old AST node at the given <paramref name="cursor"/>s position.
    /// </summary>
    /// <param name="cursor">The cursor pointing to the AST node to skip over.</param>
    private void SkipOldNode(ref Cursor cursor)
    {
        // does the cursor point to an AST node?
        if (this[cursor] is not { } current)
        {
            cursor = cursor.Finish();
            return;
        }

        // we can skip over a node if it exists in the change delta
        if (current is CXNode && current.FullTextSpan.Length + cursor.ChangeDelta < 0)
        {
            cursor = cursor with
            {
                ChangeDelta = cursor.ChangeDelta + current.FullTextSpan.Length
            };

            // move to the nodes next sibling
            MoveToNextSibling(ref cursor);

            // and update the cursors state
            SkipPastChanges(ref cursor);
            return;
        }

        // just skip the token
        SkipOldToken(ref cursor);
    }

    /// <summary>
    ///     Skips past any outdated changes in the given <paramref name="cursor"/>.
    /// </summary>
    /// <param name="cursor">The cursor containing the changes to skip over.</param>
    private void SkipPastChanges(ref Cursor cursor)
    {
        if (this[cursor] is not { } node) return;

        while (
            !cursor.Changes.IsEmpty &&
            node.FullTextSpan.Start >= cursor.Changes.Peek().Span.End
        )
        {
            var change = cursor.Changes.Peek();
            cursor = cursor with
            {
                ChangeDelta = cursor.ChangeDelta + (change.NewLength - change.Span.Length),
                Changes = cursor.Changes.Pop()
            };
        }
    }
}