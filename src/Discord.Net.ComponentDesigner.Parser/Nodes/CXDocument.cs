using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Discord.CX.Util;

namespace Discord.CX.Parser;

/// <summary>
///     Represents the root of a parsed CX AST tree. 
/// </summary>
public sealed class CXDocument : CXNode
{
    /// <summary>
    ///     Gets the parser that parsed and constructed this <see cref="CXDocument"/>.
    /// </summary>
    public CXParser Parser { get; }

    /// <summary>
    ///     Gets a collection of all the tokens found within this <see cref="CXDocument"/>.
    /// </summary>
    public IReadOnlyList<CXToken> Tokens { get; }

    /// <summary>
    ///     Gets a collection of top-level nodes in this <see cref="CXDocument"/>.
    /// </summary>
    public IReadOnlyList<CXNode> RootNodes { get; private set; }

    /// <summary>
    ///     Gets a read-only array of all the tokens that are of kind <see cref="CXTokenKind.Interpolation"/> in this
    ///     <see cref="CXDocument"/>, in order of appearance in the underlying <see cref="CXSourceText"/>.
    /// </summary>
    public ImmutableArray<CXToken> InterpolationTokens { get; }

    /// <summary>
    ///     Gets a read-only list of AST nodes that were reused if this document was incrementally parsed.
    /// </summary>
    internal IReadOnlyList<ICXNode> ReusedNodes
        => Parser
            .BlendedNodes
            .Where(x => x.Reused)
            .SelectMany(x => x.ASTNode.Walk())
            .ToArray();

    /// <summary>
    ///     Gets a read-only list of AST nodes that were newly parsed if this document was incrementally parsed.
    /// </summary>
    internal IReadOnlyList<ICXNode> NewNodes
        => [..GetFlatGraph().Except(ReusedNodes)];

    /// <summary>
    ///     Gets the <see cref="StringInternTable"/> used for interning strings in this <see cref="CXDocument"/>.
    /// </summary>
    internal StringInternTable StringTable => Parser.Reader.StringTable;

    /// <summary>
    ///     Constructs a new <see cref="CXDocument"/>.
    /// </summary>
    /// <param name="parser">The parser responsible for parsing this <see cref="CXDocument"/>.</param>
    /// <param name="rootNodes">The top-level nodes in this <see cref="CXDocument"/>.</param>
    public CXDocument(
        CXParser parser,
        IReadOnlyList<CXNode> rootNodes
    )
    {
        Parser = parser;
        Tokens = parser.Tokens;
        Slot(RootNodes = rootNodes);
        InterpolationTokens = [..parser.Lexer.InterpolationMap];
    }

    /// <summary>
    ///     Gets the index of an interpolation token found within this <see cref="CXDocument"/>.
    /// </summary>
    /// <param name="token">The interpolation token to get the index of.</param>
    /// <returns>
    ///     The index of the interpolation token within this <see cref="CXDocument"/> if found; otherwise <c>-1</c>
    /// </returns>
    public int GetInterpolationIndex(CXToken token)
    {
        if (token.Kind is not CXTokenKind.Interpolation) return -1;
        return InterpolationTokens.IndexOf(token);
    }

    /// <summary>
    ///     Checks whether a token is an interpolation token within this <see cref="CXDocument"/>.
    /// </summary>
    /// <param name="token">The token to check.</param>
    /// <returns>
    ///     <see langword="true"/> if the provided token is an interpolation within this document; otherwise
    ///     <see langword="false"/>.
    /// </returns>
    public bool IsInterpolation(CXToken token) => TryGetInterpolationIndex(token, out _);
    
    /// <summary>
    ///     Attempts to get the index of an interpolated token within this <see cref="CXDocument"/>.
    /// </summary>
    /// <param name="token">The token to get the index of.</param>
    /// <param name="index">The index of the given token.</param>
    /// <returns>
    ///     <see langword="true"/> if the token is an interpolation within this document; otherwise
    ///     <see langword="false"/>.
    /// </returns>
    public bool TryGetInterpolationIndex(CXToken token, out int index)
    {
        index = GetInterpolationIndex(token);
        return index != -1;
    }

    /// <summary>
    ///     Incrementally parses a new <see cref="CXDocument"/> with the given <see cref="CXSourceReader"/>.
    /// </summary>
    /// <param name="reader">The reader to incrementally parse from.</param>
    /// <param name="changes">A read-only collection of changes that differ between the reader and the document.</param>
    /// <param name="token">A cancellation token used to cancel incremental parsing.</param>
    /// <returns>A new <see cref="CXDocument"/> containing the new AST tree.</returns>
    [Obsolete("This is untested and most likely doesn't work anymore")]
    public CXDocument IncrementalParse(
        CXSourceReader reader,
        IReadOnlyList<CXTextChange> changes,
        CancellationToken token = default
    )
    {
        var affectedRange = CXTextChangeRange.Collapse(changes.Select(x => (CXTextChangeRange)x));

        var parser = new CXParser(reader, this, affectedRange, token);

        var rootNodes = parser
            .ParseTopLevelNodes()
            .ToImmutableList();

        return new CXDocument(parser, rootNodes);
    }

    /// <summary>
    ///     Gets a flat version of the AST tree using a DFS approach, returning terminal and non-terminal nodes in
    ///     order of appearance within the AST tree.
    /// </summary>
    /// <returns>
    ///     The flat version of this <see cref="CXDocument"/>s AST tree.
    /// </returns>
    public IReadOnlyList<ICXNode> GetFlatGraph()
    {
        var result = new List<ICXNode>();

        var stack = new Stack<(ICXNode Node, int SlotIndex)>([(this, 0)]);

        while (stack.Count > 0)
        {
            var (node, index) = stack.Pop();

            if (node is CXToken token)
            {
                result.Add(token);
                continue;
            }

            if (node is CXNode concreteNode)
            {
                if(index is 0) result.Add(node);

                if (concreteNode.Slots.Count > index)
                {
                    // enqueue self
                    stack.Push(
                        (concreteNode, index + 1)
                    );

                    // enqueue child
                    stack.Push(
                        (concreteNode.Slots[index], 0)
                    );

                    continue;
                }

                // we do nothing
            }
        }

        return result;
    }
}
