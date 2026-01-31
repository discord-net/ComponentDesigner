using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Discord.CX.Parser;

/// <summary>
///     Represents a parser that can parse the CX language into a <see cref="CXDocument"/>.
/// </summary>
public sealed partial class CXParser
{
    /// <summary>
    ///     Gets the lexer used to lex the underlying <see cref="CXSourceText"/>.
    /// </summary>
    public CXLexer Lexer { get; }

    /// <summary>
    ///     Gets the underlying <see cref="CXSourceReader"/> used by the parser.
    /// </summary>
    public CXSourceReader Reader { get; }

    /// <summary>
    ///     Gets whether this parser is operating in an incremental mode.
    /// </summary>
    /// <remarks>
    ///     In incremental mode, the parser reuses unchanged AST nodes from a previous parse,
    ///     dramatically improving performance for editor scenarios where only small regions change.
    ///     When false, a complete reparse is performed from scratch.
    /// </remarks>
    [MemberNotNullWhen(true, nameof(Blender), nameof(_blendedNodes))]
    public bool IsIncremental => Blender is not null && _blendedNodes is not null;

    /// <summary>
    ///     Gets the incremental blender used to blend an old <see cref="CXDocument"/> with new <see cref="ICXNode"/>s.
    /// </summary>
    /// <remarks>
    ///     Always returns <see langword="null"/> if <see cref="IsIncremental"/> is <see langword="false"/>.
    /// </remarks>
    public CXBlender? Blender { get; }

    /// <summary>
    ///     Gets the <see cref="CancellationToken"/> used by the <see cref="CXParser"/> and <see cref="CXLexer"/>.
    /// </summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>
    ///     Constructs a new <see cref="CXParser"/>.
    /// </summary>
    /// <param name="reader">The reader to use when parsing/lexing</param>
    /// <param name="token">
    ///     A <see cref="CancellationToken"/> used to cancel parsing.
    /// </param>
    public CXParser(CXSourceReader reader, CancellationToken token = default)
    {
        CancellationToken = token;
        Reader = reader;
        Lexer = new CXLexer(Reader, token);
        _tokens = [];
    }

    /// <summary>
    ///     Constructs a new <see cref="CXParser"/> in an incremental mode.
    /// </summary>
    /// <param name="reader">The reader to use when parsing/lexing</param>
    /// <param name="document">The old <see cref="CXDocument"/> to blend.</param>
    /// <param name="change">
    ///     A <see cref="CXTextChangeRange"/> representing what has changed within the <paramref name="document"/>.
    /// </param>
    /// <param name="token">
    ///     A <see cref="CancellationToken"/> used to cancel parsing.
    /// </param>
    public CXParser(
        CXSourceReader reader,
        CXDocument document,
        CXTextChangeRange change,
        CancellationToken token = default
    ) : this(reader, token)
    {
        Blender = new CXBlender(Lexer, document, change);
        _blendedNodes = new BlendedNode[document.GraphWidth];
    }

    /// <summary>
    ///     Parses a <see cref="CXDocument"/> from a <see cref="CXSourceReader"/>.
    /// </summary>
    /// <param name="reader">The reader to use when parsing/lexing</param>
    /// <param name="token">A <see cref="CancellationToken"/> used to cancel parsing.</param>
    /// <returns>
    ///     A <see cref="CXDocument"/> containing the AST parsed from the provided <paramref name="reader"/>.
    /// </returns>
    public static CXDocument Parse(CXSourceReader reader, CancellationToken token = default)
    {
        var parser = new CXParser(reader, token: token);

        return new CXDocument(parser, [..parser.ParseTopLevelNodes()]);
    }

    /// <summary>
    ///     Parses valid top-level nodes until a terminal node is found or the EOF is reached.
    /// </summary>
    /// <returns>
    ///     An <see cref="IEnumerable{CXNode}"/> representing the parsing of root nodes. 
    /// </returns>
    internal IEnumerable<CXNode> ParseTopLevelNodes()
    {
        using var _ = Lexer.SetMode(CXLexer.LexMode.ElementValue);

        while (CurrentToken.Kind is not CXTokenKind.EOF and not CXTokenKind.Invalid)
        {
            var node = ParseTopLevelNode();
            yield return node;
            CancellationToken.ThrowIfCancellationRequested();
            if (node.Width is 0) yield break;
        }
    }

    /// <summary>
    ///     Parses a single top-level node.
    /// </summary>
    /// <returns>
    ///     The parsed, top-level <see cref="CXNode"/> if any; otherwise a <see cref="CXValue.Invalid"/>
    /// </returns>
    internal CXNode ParseTopLevelNode()
    {
        if (TryEatASTNode<CXNode>(out var node)) return node;

        using var _ = Lexer.SetMode(CXLexer.LexMode.ElementValue);
        switch (CurrentToken.Kind)
        {
            case CXTokenKind.LessThan: return ParseElement();
            default:
                if (TryParseElementChild([], out var child))
                    return child;

                break;
        }

        return new CXValue.Invalid()
        {
            DiagnosticDescriptors =
            [
                CXDiagnosticDescriptor.InvalidRootElement(CurrentToken)
            ]
        };
    }

    /// <summary>
    ///     Parses a CX element.
    /// </summary>
    /// <remarks>
    ///     Recoverable methods are used to parse the <see cref="CXElement"/> and always returns an instance. Any
    ///     parsing errors will be propagated to the <see cref="CXNode.DiagnosticDescriptors"/> property.
    /// </remarks>
    /// <returns>
    ///     The <see cref="CXElement"/> that was parsed.
    /// </returns>
    internal CXElement ParseElement()
    {
        // reset the lexer mode to default.
        using var _ = Lexer.SetMode(CXLexer.LexMode.Default);

        // check for incremental node
        if (TryEatASTNode<CXElement>(out var node)) return node;

        var diagnostics = new List<CXDiagnosticDescriptor>();

        var start = Expect(CXTokenKind.LessThan);

        if (start.IsMissing)
        {
            // bail early, no starting token
            return new CXElement(
                start,
                new CXIdentifier.Simple(CXToken.CreateMissing(CXTokenKind.Identifier)),
                [],
                CXToken.CreateMissing(CXTokenKind.ForwardSlashGreaterThan),
                []
            );
        }

        var identifier = ParseIdentifier(includeDiagnostics: false);

        if (identifier is CXIdentifier.Simple { Token.IsMissing: true })
        {
            // bail early
            return new CXElement(
                start,
                identifier,
                [],
                CXToken.CreateMissing(CXTokenKind.ForwardSlashGreaterThan),
                []
            )
            {
                DiagnosticDescriptors =
                [
                    new CXDiagnosticDescriptor(
                        DiagnosticSeverity.Error,
                        CXErrorCode.MissingElementIdentifier,
                        "Missing identifier for element"
                    )
                ]
            };
        }

        var attributes = ParseAttributes();

        switch (CurrentToken.Kind)
        {
            // the element ends with a '>', we expect a children or a closing tag.
            case CXTokenKind.GreaterThan:
                var end = Eat();
                // parse children
                var children = ParseElementChildren();

                ParseClosingElement(
                    end,
                    diagnostics,
                    out var endStart,
                    out var endIdent,
                    out var endClose
                );

                var element = new CXElement(
                    start,
                    identifier,
                    attributes,
                    end,
                    children,
                    endStart,
                    endIdent,
                    endClose
                ) { DiagnosticDescriptors = diagnostics };


                return element;

            // we should see a '/>' which indicates the end of this element, we'll just expect that token
            case CXTokenKind.ForwardSlashGreaterThan:
            default:
                return new CXElement(
                    start,
                    identifier,
                    attributes,
                    Expect(
                        CXTokenKind.ForwardSlashGreaterThan,
                        message: "expecting '>' or '/>' to close the element tag"
                    ),
                    new()
                );
        }

        void ParseClosingElement(
            CXToken elementEnd,
            List<CXDiagnosticDescriptor> diagnostics,
            out CXToken elementEndStart,
            out CXToken? elementEndIdent,
            out CXToken elementEndClose
        )
        {
            /*
             * store a sentinel to roll back to if the element closing tag is semantically incorrect. We'll roll back
             * in that case and assume that the tokens consumed were not meant for us.
             */
            var sentinel = _position;

            // we should see a '</' token
            elementEndStart = Expect(CXTokenKind.LessThanForwardSlash, withDiagnostics: false);

            if (identifier is null)
            {
                // it's a fragment, don't expect a name
                elementEndIdent = null;
            }
            else
            {
                // we should expect an identifier
                elementEndIdent = ExpectIdentifier(withDiagnostics: false);
            }

            // we finally should see a '>'
            elementEndClose = Expect(CXTokenKind.GreaterThan, withDiagnostics: false);

            // determine if any of the tokens are missing
            var missingStructure
                = elementEndStart.IsMissing ||
                  (elementEndIdent?.IsMissing ?? false) ||
                  elementEndClose.IsMissing;

            /*
             * determine if the identifier doesn't match what we expected
             *
             * for fragments:     we expect no the identifier we just read to be null
             * for non-fragments: we expect the identifier to match what we read earlier
             */
            var missingNamedClosing = identifier is null
                ? elementEndIdent is not null
                : elementEndIdent is null || identifier.Value != elementEndIdent.RawValue;

            if (
                missingNamedClosing ||
                missingStructure
            )
            {
                // add the diagnostic to the node
                diagnostics.Add(CXDiagnosticDescriptor.MissingElementClosingTag(identifier));

                // rollback
                _position = sentinel;
            }
        }
    }

    /// <summary>
    ///     Parses a <see cref="CXIdentifier"/> AST node.
    /// </summary>
    /// <remarks>
    ///     In the case that the identifier would belong to a fragment element, <see langword="null"/> is returned;
    ///     otherwise normal identifier parsing happens, with diagnostics controlled by
    ///     <paramref name="includeDiagnostics"/>.
    /// </remarks>
    /// <param name="includeDiagnostics">Whether to include diagnostics.</param>
    /// <returns>The identifier AST node; <see langword="null"/> if it would belong to a fragment.</returns>
    internal CXIdentifier? ParseIdentifier(bool includeDiagnostics = true)
    {
        // check for incremental node
        if (TryEatASTNode<CXIdentifier>(out var node)) return node;

        using var _ = Lexer.SetMode(CXLexer.LexMode.Identifier);

        switch (CurrentToken.Kind)
        {
            // attribute start: the identifier belongs to a fragment
            case CXTokenKind.Identifier when NextToken.Kind is CXTokenKind.Equals: return null;

            // element ending tag, also a fragment
            case CXTokenKind.GreaterThan: return null;

            // interpolated identifier
            case CXTokenKind.Interpolation:
                return new CXIdentifier.Interpolated(
                    Eat(),
                    Expect(CXTokenKind.Qualifier),
                    Expect(CXTokenKind.Identifier)
                );


            // simple identifier
            case CXTokenKind.Identifier:
            default:
                return new CXIdentifier.Simple(
                    Expect(CXTokenKind.Identifier, withDiagnostics: includeDiagnostics)
                );
        }
    }

    /// <summary>
    ///     Parses an elements children. This includes the following AST nodes:<br/>
    ///         - <see cref="CXElement"/><br/>
    ///         - any <see cref="CXValue"/>
    /// </summary>
    /// <returns>
    ///     A <see cref="CXCollection{CXNode}"/> containing the children parsed, with an empty collection indicating
    ///     no children were parsed.
    /// </returns>
    internal CXCollection<CXNode> ParseElementChildren()
    {
        // check for incremental node
        if (TryEatASTNode<CXCollection<CXNode>>(out var node)) return node;

        var children = new List<CXNode>();
        var diagnostics = new List<CXDiagnosticDescriptor>();

        // set the lexer mode to element values
        using (Lexer.SetMode(CXLexer.LexMode.ElementValue))
        {
            while (TryParseElementChild(diagnostics, out var child))
            {
                children.Add(child);
                CancellationToken.ThrowIfCancellationRequested();
            }
        }

        return new CXCollection<CXNode>(children) { DiagnosticDescriptors = diagnostics };
    }

    /// <summary>
    ///     Attempts to parse an elements child node, being either a <see cref="CXElement"/> or a <see cref="CXValue"/>
    /// </summary>
    /// <param name="diagnostics">
    ///     A collection to add diagnostics to relating to parsing the node.
    /// </param>
    /// <param name="node">
    ///     The node that was parsed, or <see langword="null"/> if no node was parsed.
    /// </param>
    /// <returns>
    ///     <see langword="true"/> if a node was successfully parsed; otherwise <see langword="false"/>.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///     An unhandled token kind was included for a <see cref="CXValue.Multipart"/>.
    /// </exception>
    internal bool TryParseElementChild(List<CXDiagnosticDescriptor> diagnostics, out CXNode node)
    {
        // check for incremental node
        if (IsIncremental && CurrentNode is CXValue or CXElement)
        {
            node = EatNode();
            return true;
        }

        var parts = new List<CXToken>();

        while (CurrentToken.Kind is not CXTokenKind.EOF and not CXTokenKind.Invalid)
        {
            switch (CurrentToken.Kind)
            {
                // text & interpolation are valid for element children.
                case CXTokenKind.Interpolation:
                case CXTokenKind.Text:
                    parts.Add(Eat());
                    continue;

                // the start of another element
                case CXTokenKind.LessThan:
                    // if we have any parts accumulated already, return that instead of parsing the element.
                    node = parts.Count > 0
                        ? BuildParts()
                        : ParseElement();
                    return true;

                // any tokens that should cause us to break out
                case CXTokenKind.LessThanForwardSlash:
                case CXTokenKind.EOF:
                case CXTokenKind.Invalid: goto end;

                // we've got something we're not expecting, add a diagnostic and get out.
                default:
                    if (parts.Count is 0)
                        diagnostics.Add(CXDiagnosticDescriptor.InvalidElementChildToken(CurrentToken));

                    goto case CXTokenKind.Invalid;
            }
        }

        end:

        // did we find some valid parts?
        if (parts.Count > 0)
        {
            // build them into a value and return it.
            node = BuildParts();
            return true;
        }

        // we couldn't parse anything
        node = null!;
        return false;

        CXValue BuildParts()
        {
            // in case we tried building a value with no parts
            if (parts.Count is 0) return new CXValue.Invalid();


            // we can create the proper value type with a single part
            if (parts.Count is 1)
            {
                var token = parts[0];
                return parts[0].Kind switch
                {
                    CXTokenKind.Interpolation => new CXValue.Interpolation(
                        token,
                        Lexer.InterpolationMap.IndexOf(token)
                    ),
                    CXTokenKind.Text => new CXValue.Scalar(token),
                    _ => throw new InvalidOperationException($"Unknown single value kind '{token.Kind}'")
                };
            }

            // otherwise, create a multipart
            return new CXValue.Multipart(new(parts));
        }
    }

    /// <summary>
    ///     Parses a collection of attributes.
    /// </summary>
    /// <returns>
    ///     A <see cref="CXCollection{CXAttribute}"/> containing the attributes parsed, with an empty collection
    ///     indicating no attributes were parsed.
    /// </returns>
    internal CXCollection<CXAttribute> ParseAttributes()
    {
        // check for incremental node
        if (TryEatASTNode<CXCollection<CXAttribute>>(out var node)) return node;

        var attributes = new List<CXAttribute>();

        /*
         * Set the lexer mode to identifier, since all attributes start with an identifier.
         *
         * Even though the `ParseAttribute` function does this, we still have to since we check against the
         * `CurrentToken`, which may cause the lexer to lex a new token.
         */
        using (Lexer.SetMode(CXLexer.LexMode.Identifier))
        {
            while (CurrentToken.Kind is CXTokenKind.Identifier)
            {
                attributes.Add(ParseAttribute());
                CancellationToken.ThrowIfCancellationRequested();
            }
        }

        return new CXCollection<CXAttribute>(attributes);
    }

    /// <summary>
    ///     Parses a single <see cref="CXAttribute"/>.
    /// </summary>
    /// <remarks>
    ///     Recoverable methods are used to parse the <see cref="CXAttribute"/> and will always return an instance.
    ///     Any parsing errors will be propagated to the <see cref="CXNode.DiagnosticDescriptors"/> property.
    /// </remarks>
    /// <returns>
    ///     The parsed <see cref="CXAttribute"/>
    /// </returns>
    internal CXAttribute ParseAttribute()
    {
        // check for incremental node
        if (TryEatASTNode<CXAttribute>(out var node)) return node;

        // set the lexing mode attributes
        using (Lexer.SetMode(CXLexer.LexMode.Attribute))
        {
            // the name of the attribute
            var identifier = ExpectIdentifier();

            // try to eat an equals token, attributes may not always have values
            if (!Eat(CXTokenKind.Equals, out var equalsToken))
            {
                // no equals token, a simple name-only attribute
                return new CXAttribute(
                    identifier,
                    null,
                    null
                );
            }

            // parse the attribute value
            var value = ParseAttributeValue();

            var diagnostics = value is CXValue.Invalid
                ? [CXDiagnosticDescriptor.MissingAttributeValue]
                : Array.Empty<CXDiagnosticDescriptor>();

            return new CXAttribute(
                identifier,
                equalsToken,
                value
            )
            {
                DiagnosticDescriptors = diagnostics
            };
        }
    }

    /// <summary>
    ///     Parses a <see cref="CXValue"/> in the context of being an <see cref="CXAttribute"/>s value.
    /// </summary>
    /// <returns>
    ///     The parsed <see cref="CXValue"/>, with an invalid value being indicated by returning a
    ///     <see cref="CXValue.Invalid"/>.
    /// </returns>
    internal CXValue ParseAttributeValue()
    {
        // check for incremental node
        if (TryEatASTNode<CXValue>(out var node)) return node;

        // ensure were lexing in the attribute mode
        using (Lexer.SetMode(CXLexer.LexMode.Attribute))
        {
            switch (CurrentToken.Kind)
            {
                // '(' indicates an inline element, parse as such
                case CXTokenKind.OpenParenthesis:
                    return new CXValue.Element(
                        Eat(),
                        ParseElement(),
                        Expect(CXTokenKind.CloseParenthesis)
                    );

                // an interpolation value
                case CXTokenKind.Interpolation:
                    return new CXValue.Interpolation(
                        Eat(),
                        Lexer.InterpolationIndex!.Value
                    );

                // the start of a string literal, parse as such
                case CXTokenKind.StringLiteralStart:
                    return ParseStringLiteral();

                // unsupported value
                default:
                    return new CXValue.Invalid();
            }
        }
    }

    /// <summary>
    ///     Parses a string literal.
    /// </summary>
    /// <remarks>
    ///     Recoverable methods are used to parse the <see cref="CXValue.StringLiteral"/> and will always return an
    ///     instance. Any parsing errors will be propagated to the <see cref="CXNode.DiagnosticDescriptors"/>
    ///     property.
    /// </remarks>
    /// <returns>
    ///     A <see cref="CXValue.StringLiteral"/> node.
    /// </returns>
    internal CXValue.StringLiteral ParseStringLiteral()
    {
        // check for incremental node
        if (TryEatASTNode<CXValue.StringLiteral>(out var node)) return node;

        var diagnostics = new List<CXDiagnosticDescriptor>();
        var tokens = new List<CXToken>();

        // expect a string literal start
        var stringLiteralStartToken = Expect(CXTokenKind.StringLiteralStart);

        // set the lexing mode to string literal
        using var _ = Lexer.SetMode(CXLexer.LexMode.StringLiteral);

        /*
         * Update the lexers `QuoteChar` state so it knows what the ending character to look for is.
         *
         * It's also important to node that we grab the last character of the `stringLiteralStartToken`s value,
         * since depending on the external context of the source, an escaped quote may be a valid starting token.
         * The lexer handles finding the end token based on that context, so we don't need to do anything extra here.
         */
        Lexer.QuoteChar = stringLiteralStartToken.RawValue[stringLiteralStartToken.RawValue.Length - 1];

        while (CurrentToken.Kind is not CXTokenKind.StringLiteralEnd)
        {
            CancellationToken.ThrowIfCancellationRequested();

            switch (CurrentToken.Kind)
            {
                // text and interpolations are valid tokens of a string literal
                case CXTokenKind.Text:
                case CXTokenKind.Interpolation:
                    tokens.Add(Eat());
                    continue;

                // out bail tokens
                case CXTokenKind.Invalid or CXTokenKind.EOF: goto end;

                // anything else is not expected
                default:
                    diagnostics.Add(
                        CXDiagnosticDescriptor.InvalidStringLiteralToken(CurrentToken)
                    );
                    goto end;
            }
        }

        end:
        // we should expect a string literal end by this point
        var stringLiteralEndToken = Expect(CXTokenKind.StringLiteralEnd);

        return new CXValue.StringLiteral(
            stringLiteralStartToken,
            new CXCollection<CXToken>(tokens),
            stringLiteralEndToken
        ) { DiagnosticDescriptors = diagnostics };
    }

    /// <summary>
    ///     Expects the current token to be an identifier.
    /// </summary>
    /// <param name="withDiagnostics">Whether to include an unexpected token diagnostic.</param>
    /// <returns>
    ///     A <see cref="CXToken"/> representing the identifier, with the flags of the <see cref="CXToken"/> indicating
    ///     whether one was found. 
    /// </returns>
    internal CXToken ExpectIdentifier(bool withDiagnostics = true)
    {
        using (Lexer.SetMode(CXLexer.LexMode.Identifier))
        {
            return Expect(CXTokenKind.Identifier, withDiagnostics);
        }
    }

    /// <summary>
    ///      Returns the <see cref="CurrentToken"/> and advances to the next token.
    /// </summary>
    internal CXToken Eat()
    {
        var token = CurrentToken;
        MoveToNextToken();
        return token;
    }

    /// <summary>
    ///     Advances to the next token if the <see cref="CurrentToken"/>s <see cref="CXTokenKind"/> matches the provided
    ///     <paramref name="kind"/>.
    /// </summary>
    /// <param name="kind">The kind of token to eat.</param>
    /// <param name="token">The token that was checked against.</param>
    /// <returns>
    ///     <see langword="true"/> if the <see cref="CurrentToken"/>s kind matched the provided <paramref name="kind"/>;
    ///     otherwise <see langword="false"/>.
    /// </returns>
    internal bool Eat(CXTokenKind kind, out CXToken token)
    {
        token = CurrentToken;

        if (token.Kind == kind)
        {
            MoveToNextToken();
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Checks the <see cref="CurrentToken"/>s kind against the provided <paramref name="kinds"/>, and advances
    ///     to the next token if matched, otherwise creates a new token with the first kind and sets the
    ///     <see cref="CXTokenFlags.Missing"/> flag.
    /// </summary>
    /// <param name="kinds">A collection of <see cref="CXTokenKind"/> to match.</param>
    /// <exception cref="InvalidOperationException">
    ///     The provided <see cref="kinds"/> collection was empty.
    /// </exception>
    internal CXToken Expect(params ReadOnlySpan<CXTokenKind> kinds)
    {
        var current = CurrentToken;

        switch (kinds.Length)
        {
            case 0: throw new InvalidOperationException("Missing expected token");
            case 1: return Expect(kinds[0]);
            default:
                foreach (var kind in kinds)
                {
                    if (current.Kind == kind) return Eat();
                }

                return CXToken.CreateMissing(
                    kinds[0],
                    current.RawValue,
                    current.LeadingTrivia,
                    current.TrailingTrivia,
                    CXDiagnosticDescriptor.UnexpectedToken(current, expected: kinds.ToArray())
                );
        }
    }

    /// <summary>
    ///     Checks the <see cref="CurrentToken"/>s kind against the provided <paramref name="kind"/>, and advances to
    ///     the next token if they match; otherwise creates a new token of the provided <paramref name="kind"/> and sets
    ///     the <see cref="CXTokenFlags.Missing"/> flag.
    /// </summary>
    /// <param name="kind">The <see cref="CXTokenKind"/> to expect.</param>
    /// <param name="withDiagnostics">Whether to include an unexpected token diagnostic.</param>
    /// <param name="message">The message to use for the diagnostic.</param>
    internal CXToken Expect(CXTokenKind kind, bool withDiagnostics = true, string? message = null)
    {
        var token = CurrentToken;

        if (token.Kind != kind)
        {
            return CXToken.CreateMissing(
                kind,
                token.RawValue,
                diagnostics: withDiagnostics
                    ? [CXDiagnosticDescriptor.UnexpectedToken(token, message, kind)]
                    : []
            );
        }

        MoveToNextToken();
        return token;
    }
}