namespace Discord.CX.Parser;

/// <summary>
///     An AST node representing an identifier.
/// </summary>
public abstract class CXIdentifier : CXNode
{
    /// <summary>
    ///     Gets the simple form value of this identifier.
    /// </summary>
    public abstract string Value { get; }

    /// <summary>
    ///     An AST node representing a simple identifier.
    /// </summary>
    public sealed class Simple : CXIdentifier
    {
        /// <inheritdoc/>
        public override string Value => Token.Value;

        /// <summary>
        ///     Gets the underlying token of this identifier.
        /// </summary>
        public CXToken Token { get; }

        /// <summary>
        ///     Constructs a new simple identifier.
        /// </summary>
        /// <param name="token">The underlying token.</param>
        public Simple(CXToken token)
        {
            Slot(Token = token);
        }
    }

    /// <summary>
    ///     An AST node representing an identifier with a starting interpolation.
    /// </summary>
    public sealed class Interpolated : CXIdentifier
    {
        /// <inheritdoc/>
        public override string Value => IdentifierToken.Value;

        /// <summary>
        ///     Gets the leading interpolation token.
        /// </summary>
        public CXToken InterpolationToken { get; }
        
        /// <summary>
        ///     Gets the qualifier token separating the interpolation and identifier.
        /// </summary>
        public CXToken QualifierToken { get; }
        
        /// <summary>
        ///     Gets the identifier token.
        /// </summary>
        public CXToken IdentifierToken { get; }

        /// <summary>
        ///     Constructs a new interpolated identifier.
        /// </summary>
        /// <param name="interpolation">The interpolation token.</param>
        /// <param name="qualifier">The qualifier token.</param>
        /// <param name="identifier">The identifier token.</param>
        public Interpolated(
            CXToken interpolation,
            CXToken qualifier,
            CXToken identifier
        )
        {
            Slot(InterpolationToken = interpolation);
            Slot(QualifierToken = qualifier);
            Slot(IdentifierToken = identifier);
        }
    }
}