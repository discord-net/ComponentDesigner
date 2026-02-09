using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using System.Threading;
using ComponentDesigner;

namespace ComponentDesigner.Parser;

/// <summary>
///     Represents a lexer that can lex the CX syntax.
/// </summary>
public sealed partial class CXLexer
{
    /// <summary>
    ///     A <see langword="ref"/> <see langword="struct"/> containing the state for scanning tokens.
    /// </summary>
    private ref struct TokenInfo
    {
        /// <summary>
        ///     The starting characters index of the token.
        /// </summary>
        public int Start;

        /// <summary>
        ///     The ending characters index of the token.
        /// </summary>
        public int End;

        /// <summary>
        ///     The tokens kind.
        /// </summary>
        public CXTokenKind Kind;

        /// <summary>
        ///     The tokens flags.
        /// </summary>
        public CXTokenFlags Flags;

        /// <summary>
        ///     The text of the token.
        /// </summary>
        /// <remarks>
        ///     If unset, the <see cref="Next"/> function will populate it.
        /// </remarks>
        public string? Text;
    }

    /// <summary>
    ///     An enum defining the different modes the lexer can operate and scan tokens in.
    /// </summary>
    public enum LexMode
    {
        /// <summary>
        ///     The default lexing mode with no special context.
        /// </summary>
        Default,

        /// <summary>
        ///     The string literal mode, lexing either <see cref="CXTokenKind.Text"/>,
        ///     <see cref="CXTokenKind.Interpolation"/>, or <see cref="CXTokenKind.StringLiteralEnd"/> tokens in the
        ///     context of being in a string literal.
        /// </summary>
        StringLiteral,

        /// <summary>
        ///     The identifier lexing mode, opting to scan for <see cref="CXTokenKind.Identifier"/>s instead of text,
        ///     with identifier grammar rules enabled. This mode still allows other tokens to be scanned.
        /// </summary>
        Identifier,

        /// <summary>
        ///     The element value mode, allowing the scanning for <see cref="CXTokenKind.Text"/> tokens in the context
        ///     of an elements body.
        /// </summary>
        ElementValue,

        /// <summary>
        ///     The attribute mode, enables identifier scanning and attribute value like parenthesis and
        ///     <see cref="CXTokenKind.StringLiteralStart"/>.
        /// </summary>
        Attribute
    }

    /// <summary>
    ///     A constant denoting the start of a comment in the CX syntax.
    /// </summary>
    public const string COMMENT_START = "<!--";

    /// <summary>
    ///     A constant denoting the end of a comment in the CX syntax.
    /// </summary>
    public const string COMMENT_END = "-->";

    /// <summary>
    ///     A null character used as the boundary character.
    /// </summary>
    public const char NULL_CHAR = '\0';

    /// <summary>
    ///     A newline character.
    /// </summary>
    public const char NEWLINE_CHAR = '\n';

    /// <summary>
    ///     A carriage return character.
    /// </summary>
    public const char CARRIAGE_RETURN_CHAR = '\r';

    /// <summary>
    ///     An underscore character.
    /// </summary>
    public const char UNDERSCORE_CHAR = '_';

    /// <summary>
    ///     A hyphen character.
    /// </summary>
    public const char HYPHEN_CHAR = '-';

    /// <summary>
    ///     A period character.
    /// </summary>
    public const char PERIOD_CHAR = '.';

    /// <summary>
    ///     A less than character.
    /// </summary>
    public const char LESS_THAN_CHAR = '<';

    /// <summary>
    ///     A greater than character.
    /// </summary>
    public const char GREATER_THAN_CHAR = '>';

    /// <summary>
    ///     A forward slash character.
    /// </summary>
    public const char FORWARD_SLASH_CHAR = '/';

    /// <summary>
    ///     A backslash character.
    /// </summary>
    public const char BACK_SLASH_CHAR = '\\';

    /// <summary>
    ///     An equals character.
    /// </summary>
    public const char EQUALS_CHAR = '=';

    /// <summary>
    ///     A single quote character.
    /// </summary>
    public const char QUOTE_CHAR = '\'';

    /// <summary>
    ///     A double quote character.
    /// </summary>
    public const char DOUBLE_QUOTE_CHAR = '"';

    /// <summary>
    ///     An open parenthesis character.
    /// </summary>
    public const char OPEN_PAREN_CHAR = '(';

    /// <summary>
    ///     A close parenthesis character.
    /// </summary>
    public const char CLOSE_PAREN_CHAR = ')';

    /// <summary>
    ///     An ampersand character.
    /// </summary>
    public const char AMPERSAND_CHAR = '&';

    /// <summary>
    ///     A hashtag character
    /// </summary>
    public const char HASHTAG_CHAR = '#';

    /// <summary>
    ///     A semicolon character.
    /// </summary>
    public const char SEMICOLON_CHAR = ';';

    /// <summary>
    ///     A space character.
    /// </summary>
    public const char SPACE_CHAR = ' ';

    /// <summary>
    ///     A tab character.
    /// </summary>
    public const char TAB_CHAR = '\t';

    /// <summary>
    ///     A vertical tab character.
    /// </summary>
    public const char VERTICAL_TAB_CHAR = '\v';

    /// <summary>
    ///     A form-feed character.
    /// </summary>
    public const char FORM_FEED_CHAR = '\f';

    /// <summary>
    ///     A singleton <see cref="CXTrivia.Token"/> representing the start of a comment.
    /// </summary>
    public static readonly CXTrivia.Token CommentStartTrivia = new(CXTriviaTokenKind.CommentStart, COMMENT_START);

    /// <summary>
    ///     A singleton <see cref="CXTrivia.Token"/> representing the end of a comment.
    /// </summary>
    public static readonly CXTrivia.Token CommentEndTrivia = new(CXTriviaTokenKind.CommentEnd, COMMENT_END);

    /// <summary>
    ///     Gets the reader this <see cref="CXLexer"/> will read from.
    /// </summary>
    public CXSourceReader Reader { get; }

    /// <summary>
    ///     Gets the interpolation index of the last token returned.
    /// </summary>
    /// <remarks>
    ///     If the last token retuned isn't an interpolation token, this property will be <see langword="null"/>.
    /// </remarks>
    public int? InterpolationIndex { get; private set; }

    /// <summary>
    ///     Gets a <see cref="CXTextSpan"/> representing the current interpolation boundary at the lexers current
    ///     position.
    /// </summary>
    public CXTextSpan? CurrentInterpolationSpan
    {
        get
        {
            // there's no next interpolation
            if (Reader.Interpolations.Length <= _interpolationIndex) return null;


            for (; _interpolationIndex < Reader.Interpolations.Length; _interpolationIndex++)
            {
                CancellationToken.ThrowIfCancellationRequested();

                var interpolationSpan = Reader.Interpolations[_interpolationIndex];

                if (interpolationSpan.End < Reader.Position) continue;

                // either we're in the interpolation or it's ahead of us
                if (interpolationSpan.Contains(Reader.Position)) return interpolationSpan;

                // it's ahead of us
                break;
            }

            return null;
        }
    }

    /// <summary>
    ///     Gets a <see cref="CXTextSpan"/> of the next interpolation based off of the lexers current position.
    /// </summary>
    public CXTextSpan? NextInterpolationSpan
    {
        get
        {
            // there's no next interpolation
            if (Reader.Interpolations.Length <= _nextInterpolationIndex) return null;

            // check if it's ahead of us
            var interpolationSpan = Reader.Interpolations[_nextInterpolationIndex];

            if (interpolationSpan.End > Reader.Position) return interpolationSpan;

            for (; _nextInterpolationIndex < Reader.Interpolations.Length; _nextInterpolationIndex++)
            {
                CancellationToken.ThrowIfCancellationRequested();

                interpolationSpan = Reader.Interpolations[_nextInterpolationIndex];
                if (interpolationSpan.Start > Reader.Position) break;
            }

            if (interpolationSpan.Start <= Reader.Position) return null;

            return interpolationSpan;
        }
    }

    /// <summary>
    ///     Gets the character position of the next interpolation the lexer is to process. 
    /// </summary>
    private int InterpolationBoundary
        => CurrentInterpolationSpan?.Start ??
           NextInterpolationSpan?.Start ??
           Reader.Source.Length;

    /// <summary>
    ///     Gets whether the lexer forces <see cref="DOUBLE_QUOTE_CHAR"/>s to be escaped inside string literals.
    /// </summary>
    /// <remarks>
    ///     This property is dependent on the external context of the CX syntax.
    /// </remarks>
    public bool ForcesEscapedQuotes => Reader.WrappingQuoteCount == 1;

    /// <summary>
    ///     Gets or sets the lexers current mode.
    /// </summary>
    public LexMode Mode { get; set; }

    /// <summary>
    ///     Gets the array used to store interpolation tokens as they appear in source.
    /// </summary>
    public CXToken[] InterpolationMap { get; private set; }

    /// <summary>
    ///     Gets or sets the lexers quote character used when scanning string literals.
    /// </summary>
    public char? QuoteChar { get; set; }

    /// <summary>
    ///     Gets or sets the cancellation token used to cancel lexing.
    /// </summary>
    public CancellationToken CancellationToken { get; set; }

    private int _nextInterpolationIndex;
    private int _interpolationIndex;

    /// <summary>
    ///     Constructs a new <see cref="CXLexer"/>.
    /// </summary>
    /// <param name="reader">The reader the lexer should read from.</param>
    /// <param name="cancellationToken">A cancellation token used to cancel lexing.</param>
    public CXLexer(
        CXSourceReader reader,
        CancellationToken cancellationToken = default
    )
    {
        CancellationToken = cancellationToken;
        Reader = reader;
        Mode = LexMode.Default;
        InterpolationMap = new CXToken[Reader.Interpolations.Length];
    }

    /// <summary>
    ///     Seeks the underlying <see cref="CXSourceReader"/> to the provided position and updates the lexers state.
    /// </summary>
    /// <param name="position">The position to seek to.</param>
    public void Seek(int position)
    {
        Reader.Position = position;
        _interpolationIndex = 0;
        _nextInterpolationIndex = 0;
    }

    /// <summary>
    ///     Resets the entire state of the lexer.
    /// </summary>
    public void Reset()
    {
        InterpolationMap = new CXToken[Reader.Interpolations.Length];
        _interpolationIndex = 0;
        _nextInterpolationIndex = 0;
    }

    /// <summary>
    ///     Represents a way to scope modes inside a using directive
    /// </summary>
    /// <param name="lexer">The lexer which is to be scoped.</param>
    public readonly struct ModeScope(CXLexer? lexer) : IDisposable
    {
        private readonly LexMode _mode = lexer?.Mode ?? LexMode.Default;

        /// <inheritdoc/>
        public void Dispose()
        {
            if (lexer is null) return;

            lexer.Mode = _mode;
        }
    }

    /// <summary>
    ///     Sets the current lexers mode and returns a scoping object. 
    /// </summary>
    /// <param name="mode">The mode to set the lexer to.</param>
    /// <returns>
    ///     A scoping object used to reset the lexers mode back to what it was before this function call.
    /// </returns>
    public ModeScope SetMode(LexMode mode)
    {
        if (mode == Mode) return default;

        var sentinel = new ModeScope(this);
        Mode = mode;
        return sentinel;
    }

    /// <summary>
    ///     Lexes a new token.
    /// </summary>
    /// <returns>The lexed token.</returns>
    public CXToken Next()
    {
        InterpolationIndex = null;

        var info = default(TokenInfo);

        var leading = LexLeadingTrivia();

        info.Start = Reader.Position;
        Scan(ref info);
        info.End = Reader.Position;

        var trailing = LexTrailingTrivia();

        if (!info.Kind.TryGetText(out var rawValue))
        {
            var length = info.End - info.Start;

            rawValue = length is 0
                ? string.Empty
                : Reader.GetInternedText(info.Start, info.End - info.Start);
        }

        var value = info.Text ?? rawValue;

        var token = new CXToken(
            info.Kind,
            leading,
            trailing,
            info.Flags,
            rawValue,
            value
        );

        if (info.Kind is CXTokenKind.Interpolation && InterpolationIndex.HasValue)
            InterpolationMap[InterpolationIndex.Value] = token;

        return token;
    }

    /// <summary>
    ///     Scans for a token.
    /// </summary>
    /// <param name="info">Where the scanned tokens information should be put.</param>
    private void Scan(ref TokenInfo info)
    {
        // mode specific scan patterns
        switch (Mode)
        {
            // string literal mode ONLY can be scanned using 'LexStringLiteral'
            case LexMode.StringLiteral:
                LexStringLiteral(ref info);
                return;

            // quick check for identifier, can fall back to slower path
            case LexMode.Identifier when TryScanIdentifier(ref info):
                return;

            // quick check for element value, can fall back to slower path
            case LexMode.ElementValue when TryScanElementValue(ref info):
                return;
        }

        // check for interpolations FIRST, they carry priority
        if (TryScanInterpolation(ref info)) return;

        switch (Reader.Current)
        {
            // qualifier '.' in identifier mode
            case PERIOD_CHAR when Mode is LexMode.Identifier:
                Reader.Advance();
                info.Kind = CXTokenKind.Qualifier;
                return;
            
            // open parenthesis are only valid in attributes, they represent the start of an inline element value
            case OPEN_PAREN_CHAR when Mode == LexMode.Attribute:
                Reader.Advance();
                info.Kind = CXTokenKind.OpenParenthesis;
                return;

            // close parentheses mark the end of an inline element value, only valid in attributes
            case CLOSE_PAREN_CHAR when Mode == LexMode.Attribute:
                Reader.Advance();
                info.Kind = CXTokenKind.CloseParenthesis;
                return;

            // operates in all modes, element tags
            case LESS_THAN_CHAR:
                Reader.Advance();

                // check for '</' token
                if (Reader.Current is FORWARD_SLASH_CHAR)
                {
                    info.Kind = CXTokenKind.LessThanForwardSlash;
                    Reader.Advance();
                    return;
                }

                // just a '<' token
                info.Kind = CXTokenKind.LessThan;
                return;

            // '/>' tag char, all modes
            case FORWARD_SLASH_CHAR when Reader.Next is GREATER_THAN_CHAR:
                Reader.Advance(2);
                info.Kind = CXTokenKind.ForwardSlashGreaterThan;
                return;

            // '>' tag char, all modes
            case GREATER_THAN_CHAR:
                info.Kind = CXTokenKind.GreaterThan;
                Reader.Advance();
                return;

            // '=' char
            case EQUALS_CHAR:
                info.Kind = CXTokenKind.Equals;
                Reader.Advance();
                return;

            // a null char usually means EOF, seeing it otherwise is invalid 
            case NULL_CHAR:
                if (Reader.IsEOF)
                {
                    info.Kind = CXTokenKind.EOF;
                    return;
                }

                goto default;

            default:
                if (
                    Mode is LexMode.Attribute && (
                        TryScanAttributeValue(ref info) ||

                        // when we're scanning an attributes value, and it doesn't have a value, the next logical
                        // token should be an identifier, so we scan for that.
                        TryScanIdentifier(ref info)
                    )
                ) return;

                // we didn't find anything we expected
                info.Kind = CXTokenKind.Invalid;
                return;
        }
    }

    /// <summary>
    ///     Scans for an elements value
    /// </summary>
    /// <param name="info">The token info containing the result of the scan.</param>
    /// <returns>
    ///     <see langword="true"/> if an element value was lexed; otherwise <see langword="false"/>
    /// </returns>
    private bool TryScanElementValue(ref TokenInfo info)
    {
        // make sure we scan up to the interpolation boundary
        var interpolationUpperBounds = InterpolationBoundary;
        var start = Reader.Position;
        var lastWordChar = start;

        int? valueEndIndex = null;
        StringBuilder? value = null;

        while (Reader.Position < interpolationUpperBounds)
        {
            CancellationToken.ThrowIfCancellationRequested();

            var current = Reader.Current;

            // break out if we hit either a null char or the start of a tag 
            if (current is NULL_CHAR or LESS_THAN_CHAR)
                break;

            if (!char.IsWhiteSpace(current) && !char.IsControl(current))
                lastWordChar = Reader.Position;

            // check for escape code
            if (current is AMPERSAND_CHAR)
            {
                // exit early if the next char is whitespace
                if (IsWhitespace(Reader.Next))
                {
                    Reader.Advance();
                    continue;
                }

                // we'll process the escape sequence
                Reader.Advance();
                var escapeSequenceStart = Reader.Position;
                var escapeSequenceLength = 0;

                while (Reader.Position < interpolationUpperBounds)
                {
                    var ch = Reader.Current;

                    // break out on EOF or semicolon
                    if (ch is NULL_CHAR or SEMICOLON_CHAR) break;

                    // only consume letters, digits, and hashtags
                    if (
                        !char.IsLetterOrDigit(ch) &&
                        ch is not HASHTAG_CHAR
                    ) break;

                    Reader.Advance();
                    escapeSequenceLength++;
                }

                // if there is no sequence, continue as if we just read an ampersand
                if (escapeSequenceLength is 0)
                {
                    continue;
                }

                var escapeSequenceEnd = escapeSequenceStart + escapeSequenceLength;

                // skip the semicolon if it's there
                if (Reader.Current is SEMICOLON_CHAR)
                {
                    Reader.Advance();
                    escapeSequenceEnd++;
                }

                // the actual sequence, without the ampersand and semicolon
                var sequence = Reader[escapeSequenceStart, escapeSequenceLength];

                if (TryParseEscapeSequence(sequence, out var sequenceChar))
                {
                    // if we haven't been using a builder, construct a new one and append what we've read up to this
                    // point
                    if (value is null)
                    {
                        value = new();

                        if ((start != escapeSequenceStart - 1))
                            value.Append(Reader[start, (escapeSequenceStart - start - 1)]);
                    }

                    // if the build is not null, we should have an index that points the builder to the ending char in
                    // the source
                    else if (valueEndIndex.HasValue)
                    {
                        // append up to this escape
                        var delta = escapeSequenceStart - valueEndIndex.Value - 1;
                        if (delta > 0)
                            value.Append(Reader[valueEndIndex.Value, delta]);
                    }
                    else
                    {
                        // bad state, value is not null, but we didn't set an ending index
                        throw new InvalidOperationException(
                            "Missing text value end index"
                        );
                    }

                    // append our char
                    value.Append(sequenceChar);
                    // update the value index
                    valueEndIndex = escapeSequenceEnd;

                    // also update the last word char
                    lastWordChar = escapeSequenceEnd - 1;
                    continue;
                }
            }

            Reader.Advance();
        }

        var length = Reader.Position - start;

        if (length is 0 && value is null)
        {
            // we've read nothing
            return false;
        }

        info.Kind = CXTokenKind.Text;

        if (lastWordChar + 1 != Reader.Position)
        {
            // go back to the character after the last word character
            Reader.Position = lastWordChar + 1;
        }

        // did we read into the builder
        if (value is not null)
        {
            if (valueEndIndex is null) throw new InvalidOperationException("Bad value state");

            // slice in the remaining characters
            value.Append(
                Reader[valueEndIndex.Value, (Reader.Position - valueEndIndex.Value)]
            );

            info.Text = Reader.Intern(value);
            return true;
        }

        return true;
    }

    /// <summary>
    ///     Lexes tokens in the <see cref="LexMode.StringLiteral"/> mode.
    /// </summary>
    /// <param name="info">The token info containing the result of the lex.</param>
    /// <exception cref="InvalidOperationException">The state of the lexer didn't match the lexing mode.</exception>
    private void LexStringLiteral(ref TokenInfo info)
    {
        // ensure the state matches the lexing mode, the 'QuoteChar' should always be set in string literal mode
        if (QuoteChar is null)
            throw new InvalidOperationException("Missing closing char for string literal");

        // bail early if we are at the end of the source
        if (Reader.IsEOF)
        {
            // TODO: unclosed string literal diagnostic?
            info.Kind = CXTokenKind.EOF;
            return;
        }

        // check for interpolations before assuming we can read values
        if (TryScanInterpolation(ref info)) return;

        var interpolationUpperBounds = InterpolationBoundary;

        // we only want to force escape quotes if the external context requires it AND the string literal uses 
        // a double quote character
        var forcesEscapedQuotes = ForcesEscapedQuotes && QuoteChar is DOUBLE_QUOTE_CHAR;

        if (
            /*
             * check if we're at the end of the string literal:
             *
             * if we require escaping the quote we should expect \"
             * otherwise, check for the quote char
             */
            forcesEscapedQuotes
                ? Reader.Current is BACK_SLASH_CHAR && Reader.Next == QuoteChar
                : Reader.Current == QuoteChar
        )
        {
            // consume the ending characters
            Reader.Advance(forcesEscapedQuotes ? 2 : 1);

            // set the result with an interned value 
            info.Kind = CXTokenKind.StringLiteralEnd;
            info.Text = Reader.GetInternedText(info.Start, Reader.Position - info.Start);

            // clear the state
            QuoteChar = null;
            return;
        }

        // were scanning a value of a string literal, it can be text or an interpolation so we scan up until the
        // interpolation boundary
        for (; Reader.Position < interpolationUpperBounds; Reader.Advance())
        {
            CancellationToken.ThrowIfCancellationRequested();

            if (Reader.Current is BACK_SLASH_CHAR)
            {
                // escaped backslash, advance thru the current and next character
                if (Reader.Next is BACK_SLASH_CHAR && ForcesEscapedQuotes)
                {
                    Reader.Advance();
                    continue;
                }

                // is the escaped quote forced? meaning we treat it as the ending quote to the string literal
                if (QuoteChar == Reader.Next && ForcesEscapedQuotes)
                {
                    break;
                }

                // TODO: open back slash error?
            }
            else if (QuoteChar == Reader.Current) break;
        }

        // we've finished scanning through the text, we should expect to have read at least one character since
        // we check for interpolations before scanning values
        Debug.Assert(
            info.Start < Reader.Position,
            "Lexing string literal value should have read at least one char"
        );

        info.Kind = CXTokenKind.Text;
        info.Text = Reader.GetInternedText(info.Start, Reader.Position - info.Start);
    }

    /// <summary>
    ///     Scans for an attributes value.
    /// </summary>
    /// <param name="info">The token info containing the result of the scan.</param>
    /// <returns>
    ///     <see langword="true"/> if an attributes value was read; otherwise <see langword="false"/>.
    /// </returns>
    private bool TryScanAttributeValue(ref TokenInfo info)
    {
        // bail early if were in the string literal mode
        if (Mode is LexMode.StringLiteral) return false;

        var isEscaped = ForcesEscapedQuotes && Reader.Current is BACK_SLASH_CHAR;

        // this is the gate for handling single vs double quotes:
        // single quotes *can not* be escaped as a valid starting
        // quote
        var quoteTestChar = isEscaped && Reader.Next is DOUBLE_QUOTE_CHAR
            ? Reader.Next
            : Reader.Current;

        if (quoteTestChar is not QUOTE_CHAR and not DOUBLE_QUOTE_CHAR)
        {
            // interpolations only
            return TryScanInterpolation(ref info);
        }

        // set the string literal state
        QuoteChar = quoteTestChar;
        Reader.Advance(isEscaped ? 2 : 1);
        info.Kind = CXTokenKind.StringLiteralStart;
        info.Text = Reader.Intern([quoteTestChar]);
        return true;
    }

    /// <summary>
    ///     Scan for an identifier.
    /// </summary>
    /// <param name="info">The token info containing the result of the scan.</param>
    /// <returns>
    ///     <see langword="true"/> if an identifier was read; otherwise <see langword="false"/>.
    /// </returns>
    private bool TryScanIdentifier(ref TokenInfo info)
    {
        var upperBounds = InterpolationBoundary;

        // check the current character for valid starting characters and bail out early if it's not valid or
        // an interpolation exists at the current readers position
        if (
            !IsValidIdentifierStartChar(Reader.Current) ||
            Reader.Position >= upperBounds
        ) return false;

        // eat up characters while they're valid identifiers characters
        do
        {
            Reader.Advance();
        } while (
            IsValidIdentifierChar(Reader.Current) &&
            Reader.Position < upperBounds &&
            !CancellationToken.IsCancellationRequested
        );

        CancellationToken.ThrowIfCancellationRequested();

        info.Kind = CXTokenKind.Identifier;
        info.Text = Reader.GetInternedText(info.Start, Reader.Position - info.Start);

        return true;

        static bool IsValidIdentifierChar(char c)
            => c is UNDERSCORE_CHAR or HYPHEN_CHAR or PERIOD_CHAR || char.IsLetterOrDigit(c);

        static bool IsValidIdentifierStartChar(char c)
            => c is UNDERSCORE_CHAR || char.IsLetter(c);
    }

    /// <summary>
    ///     Scans for an interpolation token.
    /// </summary>
    /// <param name="info">The token info containing the result of the scan.</param>
    /// <returns>
    ///     <see langword="true"/> if an interpolation was read; otherwise <see langword="false"/>.
    /// </returns>
    private bool TryScanInterpolation(ref TokenInfo info)
    {
        // check the current interpolation span for the token
        if (CurrentInterpolationSpan is { } span)
        {
            info.Kind = CXTokenKind.Interpolation;
            Reader.Advance(
                span.End - Reader.Position
            );
            InterpolationIndex = _interpolationIndex;

            return true;
        }

        return false;
    }

    /// <summary>
    ///     Lexes any trivia in a leading context. 
    /// </summary>
    /// <returns>The lexed trivia.</returns>
    private LexedCXTrivia LexLeadingTrivia() => LexTrivia(isTrailing: false);

    /// <summary>
    ///     Lexes any trivia in a trailing context. 
    /// </summary>
    /// <returns>The lexed trivia.</returns>
    private LexedCXTrivia LexTrailingTrivia() => LexTrivia(isTrailing: true);

    /// <summary>
    ///     Lexes trivia at the readers current position.
    /// </summary>
    /// <param name="isTrailing">Whether to lex in a trailing context.</param>
    /// <returns>The lexed trivia.</returns>
    private LexedCXTrivia LexTrivia(bool isTrailing)
    {
        // string literals have no trivia
        if (Mode is LexMode.StringLiteral) return LexedCXTrivia.Empty;

        ImmutableArray<CXTrivia>.Builder? result = null;

        var startPos = Reader.Position;

        while (!Reader.IsEOF && !CancellationToken.IsCancellationRequested)
        {
            CancellationToken.ThrowIfCancellationRequested();

            var current = Reader.Current;

            // break out if there is an interpolation at the current position
            if (CurrentInterpolationSpan is not null) break;

            // CRLF sequence
            if (current is CARRIAGE_RETURN_CHAR && Reader.Next is NEWLINE_CHAR)
            {
                Add(
                    new CXTrivia.Token(
                        CXTriviaTokenKind.Newline,
                        Reader.ReadInternedText(2)
                    )
                );

                // don't consume any more trivia if its trailing
                if (isTrailing) break;

                continue;
            }

            // basic newline
            if (current is NEWLINE_CHAR)
            {
                Add(
                    new CXTrivia.Token(
                        CXTriviaTokenKind.Newline,
                        Reader.ReadInternedText(1)
                    )
                );

                // don't consume any more trivia if its trailing
                if (isTrailing) break;

                continue;
            }

            if (IsWhitespace(current))
            {
                var start = Reader.Position;

                do
                {
                    Reader.Advance();
                } while (IsWhitespace(Reader.Current));

                Add(
                    new CXTrivia.Token(
                        CXTriviaTokenKind.Whitespace,
                        Reader.GetInternedText(start, Reader.Position - start)
                    )
                );

                continue;
            }

            if (current is LESS_THAN_CHAR && IsCurrentlyAtCommentStart())
            {
                Reader.Advance(COMMENT_START.Length);

                var commentValueStart = Reader.Position;

                while (!Reader.IsEOF && !IsCurrentlyAtCommentEnd() && !CancellationToken.IsCancellationRequested)
                {
                    Reader.Advance();
                }

                var value = new CXTrivia.Token(
                    CXTriviaTokenKind.Comment,
                    Reader.GetInternedText(commentValueStart, Reader.Position - commentValueStart)
                );

                CXTrivia.Token? end = null;

                if (IsCurrentlyAtCommentEnd())
                {
                    Reader.Advance(COMMENT_END.Length);
                    end = CommentEndTrivia;
                }

                Add(
                    new CXTrivia.XmlComment(
                        CommentStartTrivia,
                        value,
                        end
                    )
                );

                continue;
            }

            // break out if we didn't process the current character
            break;
        }

        end:
        if (result is null) return LexedCXTrivia.Empty;

        return new LexedCXTrivia(result.ToImmutable());

        void Add(CXTrivia trivia)
            => (result ??= ImmutableArray.CreateBuilder<CXTrivia>()).Add(trivia);
    }

    /// <summary>
    ///     Determines if the readers currently at a comment start syntax.
    /// </summary>
    /// <returns>
    ///     <see langword="true"/> if the reader is at the start of a comment; otherwise <see langword="false"/>.
    /// </returns>
    private bool IsCurrentlyAtCommentStart()
        => IsCommentStart(Reader.Peek(COMMENT_START.Length));

    /// <summary>
    ///     Determines if the readers currently at a comment end syntax.
    /// </summary>
    /// <returns>
    ///     <see langword="true"/> if the reader is at the end of a comment; otherwise <see langword="false"/>.
    /// </returns>
    private bool IsCurrentlyAtCommentEnd()
        => Reader.Current is HYPHEN_CHAR && IsCommentEnd(Reader.Peek(COMMENT_END.Length));

    /// <summary>
    ///     Checks if the provided string is a comment end.
    /// </summary>
    /// <param name="str">The string to check.</param>
    /// <returns>
    ///     <see langword="true"/> if the provided string is the end of a comment; otherwise <see langword="false"/>.
    /// </returns>
    internal static bool IsCommentEnd(string str)
        => str is COMMENT_END;

    /// <summary>
    ///     Checks if the provided string is a comment start.
    /// </summary>
    /// <param name="str">The string to check.</param>
    /// <returns>
    ///     <see langword="true"/> if the provided string is the start of a comment; otherwise <see langword="false"/>.
    /// </returns>
    internal static bool IsCommentStart(string str)
        => str is COMMENT_START;

    /// <summary>
    ///     Checks if the provided character is a recognized whitespace character
    /// </summary>
    /// <param name="ch">The character to check.</param>
    /// <returns>
    ///     <see langword="true"/> if the given character is a recognized whitespace character; otherwise
    ///     <see langword="false"/>.
    /// </returns>
    public static bool IsWhitespace(char ch)
        => ch
            is TAB_CHAR
            or VERTICAL_TAB_CHAR
            or FORM_FEED_CHAR
            or '\u001A'
            or SPACE_CHAR;
}