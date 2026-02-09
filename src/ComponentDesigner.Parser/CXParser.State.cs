using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace ComponentDesigner.Parser;

partial class CXParser
{
    /// <summary>
    ///     Gets or computes the currently blended non-terminal AST node.
    /// </summary>
    /// <remarks>
    ///     This property is always <see langword="null"/> when operating in a non-incremental mode.
    /// </remarks>
    private CXNode? CurrentNode
        => CurrentBlendedNode?.ASTNode as CXNode;

    /// <summary>
    ///     Gets or computes a blended non-terminal AST node.
    /// </summary>
    /// <remarks>
    ///     This property is always <see langword="null"/> when operating in a non-incremental mode.
    /// </remarks>
    private BlendedNode? CurrentBlendedNode
        => _currentBlendedNode ??= GetCurrentBlendedNode();

    /// <summary>
    ///     Gets the currently lexed token.
    /// </summary>
    public CXToken CurrentToken
        => _currentToken ??= FetchCurrentToken();

    /// <summary>
    ///     Gets the lexed token that semantically follows the <see cref="CurrentToken"/>.
    /// </summary>
    public CXToken NextToken => PeekToken(1);

    /// <summary>
    ///     Gets a collection of <see cref="ICXNode"/>s that were served by the <see cref="CXBlender"/> if the parser is
    ///     operating in an incremental mode. 
    /// </summary>
    public IReadOnlyList<BlendedNode> BlendedNodes
        => [.._blendedNodes ?? []];

    /// <summary>
    ///     Gets the collection of tokens the parser has lexed.
    /// </summary>
    public IReadOnlyList<CXToken> Tokens
        => IsIncremental
            ? [..BlendedNodes.Select(x => x.ASTNode).OfType<CXToken>()]
            : [.._tokens];

    // the non-incremental flat token array
    private readonly List<CXToken> _tokens;

    // the incremental blended nodes array
    private BlendedNode[]? _blendedNodes;

    private int _position;
    private int _count;

    // some cached state
    private CXToken? _currentToken;
    private BlendedNode? _currentBlendedNode;

    /// <summary>
    ///     Attempts to blend a non-terminal AST node, and returns whether its of the given type.
    /// </summary>
    /// <param name="node">The blended, non-terminal AST node.</param>
    /// <typeparam name="T">The expected type of the blended node.</typeparam>
    /// <returns>
    ///     <see langword="true"/> if the blender produced a non-terminal AST node of the given type; otherwise
    ///     <see langword="false"/>.
    /// </returns>
    private bool TryEatASTNode<T>([MaybeNullWhen(false)] out T node) where T : CXNode
    {
        // no work if we're not operating in an incremental mode.
        if (!IsIncremental)
        {
            node = null;
            return false;
        }

        if (CurrentNode is not T target)
        {
            node = null;
            return false;
        }

        node = (T)EatNode();
        return true;
    }

    /// <summary>
    ///     Returns the currently blended non-terminal AST node and advances the blender to the next node.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    ///     The <see cref="CurrentBlendedNode"/> is <see langword="null"/> or the parser is not operating in an
    ///     incremental mode.
    /// </exception>
    private CXNode EatNode()
    {
        var current = _currentBlendedNode;

        if (!IsIncremental || current?.ASTNode is not CXNode)
            throw new InvalidOperationException("The currently blended node is null");

        // ensure we have slots for this node
        if (_position >= _blendedNodes.Length)
            AddSlots();

        // clone the ast node such that it detaches from its existing AST tree 
        var blended = current!.Value.WithClonedASTNode();

        // store it as the head node
        _blendedNodes[_position++] = blended;
        _count = _position;

        // reset the cached state 
        _currentBlendedNode = null;
        _currentToken = null;

        return (CXNode)blended.ASTNode;
    }

    /// <summary>
    ///     Gets or computes the currently blended node. 
    /// </summary>
    /// <returns>
    ///     The current <see cref="BlendedNode"/> if any, otherwise <see langword="null"/>.
    /// </returns>
    private BlendedNode? GetCurrentBlendedNode()
        => _position is 0
            ? Blender?.Next(asToken: false, Blender.StartingCursor)
            : Blender?.Next(asToken: false, _blendedNodes![_position - 1].Cursor);

    /// <summary>
    ///     Fetches the current token either from a cache, the lexer, or the blender.
    /// </summary>
    /// <returns>The current token based off of the parsers state.</returns>
    private CXToken FetchCurrentToken()
    {
        // ensure we're not behind 
        while (_position >= _count)
            AddNewToken();

        if (IsIncremental)
        {
            /*
             * TODO:
             * There may be an edge case where the head blended node is not a token, and the above catch-up loop doesn't
             * run, in that case blending a new token could provide correct behaviour. Should we blend from the head
             * node or the previous cursor?
             */
            var node = _blendedNodes[_position].ASTNode;

            Debug.Assert(node is CXToken, "Expecting the head blended node to be a token");

            return (CXToken)node;
        }
        else
        {
            // simple index into the tokens list
            return _tokens[_position];
        }
    }

    /// <summary>
    ///     Peeks a given number of tokens ahead and returns the token at that offset.
    /// </summary>
    /// <param name="offset">The number of tokens to look ahead of.</param>
    /// <returns>The token at the provided offset.</returns>
    private CXToken PeekToken(int offset)
    {
        // fetch tokens up to the offset, not incrementing the position
        while (_position + offset >= _count)
            AddNewToken();


        if (IsIncremental)
        {
            // our token should live at the position + offset
            var node = _blendedNodes[_position + offset].ASTNode;

            Debug.Assert(node is CXToken, $"Expecting a token after blending with {nameof(AddNewToken)}");

            return (CXToken)node;
        }
        else
        {
            // token lives in the flat token list
            return _tokens[_position + offset];
        }
    }

    /// <summary>
    ///     Reads and adds a new token to this parsers cache.
    /// </summary>
    /// <remarks>
    ///     If the parser is operating in an incremental mode, the blender is used to fetch the new token; otherwise
    ///     the lexer is used.
    /// </remarks>
    private void AddNewToken()
    {
        if (IsIncremental)
        {
            /*
             * If we have existing blended nodes, use the head nodes cursor as the blending point, otherwise check
             * if the currently blended node is a token, we'll use that at our blending point.
             *
             * Fallback to the starting cursor if none of those cases are met
             */
            AddBlendedNode(
                _count > 0
                    ? Blender.Next(asToken: true, _blendedNodes[_count - 1].Cursor)
                    : _currentBlendedNode?.ASTNode is CXToken
                        ? _currentBlendedNode.Value
                        : Blender.Next(asToken: true, Blender.StartingCursor)
            );
        }
        else
        {
            // non-incremental uses the lexer
            _tokens.Add(Lexer.Next());
            _count++;
        }
    }

    /// <summary>
    ///     Advances the parsers state to the next token.
    /// </summary>
    private void MoveToNextToken()
    {
        _currentToken = null;

        if (IsIncremental)
            _currentBlendedNode = null;

        _position++;
    }

    /// <summary>
    ///     Adds a blended node to this parsers cache.
    /// </summary>
    /// <param name="blended">The blended node to add.</param>
    private void AddBlendedNode(in BlendedNode blended)
    {
        Debug.Assert(blended.ASTNode is CXToken);

        if (_count >= _blendedNodes!.Length)
            AddSlots();

        _blendedNodes[_count] = blended.WithClonedASTNode();
        _count++;
    }

    /// <summary>
    ///     Adds new slots to the <see cref="_blendedNodes"/> array.
    /// </summary>
    private void AddSlots()
    {
        Array.Resize(ref _blendedNodes, _blendedNodes!.Length * 2);
    }
}