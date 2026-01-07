namespace Discord.CX.Parser;

/// <summary>
///     An enum defining the possible types of tokens in the CX syntax.
/// </summary>
public enum CXTokenKind : byte
{
    /// <summary>
    ///     An invalid or unrecognized token.
    /// </summary>
    Invalid,
    
    /// <summary>
    ///     The EOF (end of file) token.
    /// </summary>
    EOF,

    /// <summary>
    ///     A single less than '<c>&lt;</c>' token.
    /// </summary>
    LessThan,
    
    /// <summary>
    ///     A single greater than '<c>&gt;</c>' token.
    /// </summary>
    GreaterThan,
    
    /// <summary>
    ///     A forward slash and greater than '<c>/&gt;</c>' token.
    /// </summary>
    ForwardSlashGreaterThan,
    
    /// <summary>
    ///     A less than and forward slash '<c>&lt;/</c>' token.
    /// </summary>
    LessThanForwardSlash,
    
    /// <summary>
    ///     A single equals '<c>=</c>' token.
    /// </summary>
    Equals,

    /// <summary>
    ///     A variable length text token.
    /// </summary>
    Text,
    
    /// <summary>
    ///     An interpolation token.
    /// </summary>
    Interpolation,

    /// <summary>
    ///     A token denoting the start of a string literal.
    /// </summary>
    StringLiteralStart,
    
    /// <summary>
    ///     A token denoting the start of a string literal.
    /// </summary>
    StringLiteralEnd,
    
    /// <summary>
    ///     A single open parenthesis '<c>(</c>' token.
    /// </summary>
    OpenParenthesis,
    
    /// <summary>
    ///     A single close parenthesis '<c>(</c>' token.
    /// </summary>
    CloseParenthesis,

    /// <summary>
    ///     A variable length identifier token.
    /// </summary>
    Identifier,
    
    /// <summary>
    ///     A name qualifying token '<c>.</c>'.
    /// </summary>
    Qualifier,
}