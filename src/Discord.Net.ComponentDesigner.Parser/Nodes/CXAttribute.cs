namespace Discord.CX.Parser;

/// <summary>
///     An AST node representing a single attribute within the CX language.
/// </summary>
public sealed class CXAttribute : CXNode
{
    public string Identifier => IdentifierToken.RawValue;
    
    /// <summary>
    ///     Gets the token containing the identifier for this <see cref="CXAttribute"/>.
    /// </summary>
    public CXToken IdentifierToken { get; }

    /// <summary>
    ///     Gets the equals ('=') token separating the identifier and value within this <see cref="CXAttribute"/>.
    /// </summary>
    public CXToken? EqualsToken { get; }

    /// <summary>
    ///     Gets the optional value of this <see cref="CXAttribute"/>.
    /// </summary>
    public CXValue? Value { get; }

    /// <summary>
    ///     Constructs a new <see cref="CXAttribute"/>. 
    /// </summary>
    /// <param name="identifier">The token containing the identifier.</param>
    /// <param name="equalsToken">The token separating the identifier and value.</param>
    /// <param name="value">The value of the attribute.</param>
    public CXAttribute(
        CXToken identifier,
        CXToken? equalsToken,
        CXValue? value
    )
    {
        Slot(IdentifierToken = identifier);
        Slot(EqualsToken = equalsToken);
        Slot(Value = value);
    }
}
