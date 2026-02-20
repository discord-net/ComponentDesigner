using System;

namespace ComponentDesigner.Parser;

/// <summary>
///     An abstract record representing a non-specific form of syntax trivia.
/// </summary>
public abstract record CXTrivia
{
    public static readonly Token LineBreak = new(CXTriviaTokenKind.Newline, Environment.NewLine);
    
    /// <summary>
    ///     Gets the length in characters of this trivia.
    /// </summary>
    public abstract int Length { get; }

    /// <summary>
    ///     Gets whether the trivia is considered as whitespace trivia. 
    /// </summary>
    public bool IsWhitespaceTrivia
        => this is Token { Kind: CXTriviaTokenKind.Newline or CXTriviaTokenKind.Whitespace };

    /// <summary>
    ///     Converts this trivia into its equivalent syntax
    /// </summary>
    /// <returns>The string form of this trivia.</returns>
    public abstract override string ToString();

    /// <summary>
    ///     Represents a tokenized form of syntax trivia.
    /// </summary>
    /// <param name="Kind">The kind of the syntax trivia.</param>
    /// <param name="Value">The underlying value of the syntax trivia.</param>
    public sealed record Token(
        CXTriviaTokenKind Kind,
        string Value
    ) : CXTrivia()
    {
        /// <inheritdoc/>
        public override int Length => Value.Length;
        
        /// <inheritdoc/>
        public override string ToString() => Value;
    }

    /// <summary>
    ///     Represents a XML comment syntax trivia. 
    /// </summary>
    /// <param name="Start">The starting trivia token of this comment.</param>
    /// <param name="Value">The value trivia token of this comment.</param>
    /// <param name="End">The ending trivia token of this comment.</param>
    public sealed record XmlComment(
        Token Start,
        Token Value,
        Token? End
    ) : CXTrivia
    {
        /// <inheritdoc/>
        public override int Length => Start.Length + Value.Length + (End?.Length ?? 0);
        
        /// <inheritdoc/>
        public override string ToString()
            => $"{Start}{Value}{End}";
    }
}