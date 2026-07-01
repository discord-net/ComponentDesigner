using System.Linq;

namespace ComponentDesigner.Parser;

/// <summary>
///     An AST node representing one or more values within the CX language. 
/// </summary>
public abstract class CXValue : CXNode
{
    /// <summary>
    ///     Represents an invalid value.
    /// </summary>
    public sealed class Invalid : CXValue;

    /// <summary>
    ///     Represents an array of other <see cref="CXValue"/>s.
    /// </summary>
    public sealed class Array : CXValue
    {
        /// <summary>
        ///     Gets the opening token of the array.
        /// </summary>
        public CXToken OpenBracket { get; }
        
        /// <summary>
        ///     Gets the collection of <see cref="CXValue.ArrayElement"/> within this array.
        /// </summary>
        public CXCollection<ArrayElement> Elements { get; }
        
        /// <summary>
        ///     Gets the closing toke of the array.
        /// </summary>
        public CXToken CloseBracket { get; }

        /// <summary>
        ///     Constructs a new <see cref="CXValue.Array"/>.
        /// </summary>
        /// <param name="openBracket">The opening token of the array.</param>
        /// <param name="elements">The elements of the array.</param>
        /// <param name="closeBracket">The closing token of the array.</param>
        public Array(
            CXToken openBracket,
            CXCollection<ArrayElement> elements,
            CXToken closeBracket
        )
        {
            OpenBracket = Slot(openBracket);
            Elements = Slot(elements);
            CloseBracket = Slot(closeBracket);
        }
    }
    
    /// <summary>
    ///     Represents an element within a <see cref="CXValue.Array"/>.
    /// </summary>
    public sealed class ArrayElement : CXValue
    {
        /// <summary>
        ///     Gets the underlying value of this element.
        /// </summary>
        public CXValue Value { get; }
        
        /// <summary>
        ///     Gets the right-most separator of this element.
        /// </summary>
        public CXToken? Separator { get; }

        /// <summary>
        ///     Constructs a new <see cref="CXValue.ArrayElement"/>.
        /// </summary>
        /// <param name="value">The inner value of the element.</param>
        /// <param name="separator">The right-most separator of the element.</param>
        public ArrayElement(CXValue value, CXToken? separator)
        {
            Value = Slot(value);
            Separator = Slot(separator);
        }
    }

    /// <summary>
    ///     Represents an inline element value, usually found within attributes.
    /// </summary>
    public sealed class Element : CXValue
    {
        /// <summary>
        ///     Gets the opening parenthesis token of this <see cref="CXValue.Element"/>.
        /// </summary>
        public CXToken OpenParenthesis { get; }

        /// <summary>
        ///     Gets the underlying <see cref="CXElement"/> of this <see cref="CXValue.Element"/>.
        /// </summary>
        public CXElement Value { get; }

        /// <summary>
        ///     Gets the closing parenthesis token of this <see cref="CXValue.Element"/>.
        /// </summary>
        public CXToken CloseParenthesis { get; }

        /// <summary>
        ///     Constructs a new <see cref="CXValue.Element"/>.
        /// </summary>
        /// <param name="openParenthesesToken">The open parentheses token.</param>
        /// <param name="element">The element value.</param>
        /// <param name="closeParenthesesToken">The close parentheses token.</param>
        public Element(
            CXToken openParenthesesToken,
            CXElement element,
            CXToken closeParenthesesToken
        )
        {
            Slot(OpenParenthesis = openParenthesesToken);
            Slot(Value = element);
            Slot(CloseParenthesis = closeParenthesesToken);
        }
    }

    /// <summary>
    ///     Represents a multipart value, spanning across multiple tokens.
    /// </summary>
    public class Multipart : CXValue
    {
        /// <summary>
        ///     Gets whether this <see cref="CXValue.Multipart"/> contains any
        ///     <see cref="CXTokenKind.Interpolation"/> tokens.
        /// </summary>
        public bool HasInterpolations => Tokens.Any(x => x.Kind is CXTokenKind.Interpolation);

        /// <summary>
        ///     Gets the tokens contained up this <see cref="CXValue.Multipart"/>.
        /// </summary>
        public CXCollection<CXToken> Tokens { get; }

        /// <summary>
        ///     Constructs a new <see cref="CXValue.Multipart"/>.
        /// </summary>
        /// <param name="tokens">The tokens contained in the multipart.</param>
        public Multipart(CXCollection<CXToken> tokens)
        {
            Slot(Tokens = tokens);
        }
    }

    /// <summary>
    ///     Represents a string literal value, spanning across multiple tokens.
    /// </summary>
    public sealed class StringLiteral : Multipart
    {
        /// <summary>
        ///     Gets the starting token of the string literal.
        /// </summary>
        public CXToken StartToken { get; }

        /// <summary>
        ///     Gets the ending token of the string literal.
        /// </summary>
        public CXToken EndToken { get; }

        /// <summary>
        ///     Constructs a new <see cref="CXValue.StringLiteral"/>
        /// </summary>
        /// <param name="startToken">The starting token of the string literal.</param>
        /// <param name="tokens">The tokens representing the value of the string literal.</param>
        /// <param name="endToken">The ending token of the string literal.</param>
        public StringLiteral(
            CXToken startToken,
            CXCollection<CXToken> tokens,
            CXToken endToken
        ) : base(tokens)
        {
            Slot(StartToken = startToken);
            Slot(EndToken = endToken);

            // hack: we flip the slot order due to inheritance with the constructor
            SwapSlots(0, 1);
        }
    }

    /// <summary>
    ///     Represents an interpolated value.
    /// </summary>
    public sealed class Interpolation : CXValue
    {
        /// <summary>
        ///     Gets the underlying token that represents the interpolation.
        /// </summary>
        public CXToken Token { get; }

        /// <summary>
        ///     Gets the index of the interpolation, as defined by <see cref="CXDocument.InterpolationTokens"/>.
        /// </summary>
        public int InterpolationIndex { get; }

        /// <summary>
        ///     Constructs a new <see cref="CXValue.Interpolation"/>.
        /// </summary>
        /// <param name="token">The underlying token.</param>
        /// <param name="interpolationIndex">The index of the interpolation.</param>
        public Interpolation(CXToken token, int interpolationIndex)
        {
            Slot(Token = token);
            InterpolationIndex = interpolationIndex;
        }
    }

    /// <summary>
    ///     Represents some scalar text.
    /// </summary>
    public sealed class Scalar : CXValue
    {
        /// <summary>
        ///     Gets the full value including trivia of this <see cref="CXValue.Scalar"/>.
        /// </summary>
        public string FullValue => Token.ToFullString();

        /// <summary>
        ///     Gets the value of this <see cref="CXValue.Scalar"/>.
        /// </summary>
        public string Value => Token.Value;

        /// <summary>
        ///     Gets the underlying token that this <see cref="CXValue.Scalar"/> represents.
        /// </summary>
        public CXToken Token { get; }


        /// <summary>
        ///     Constructs a new <see cref="CXValue.Scalar"/>.
        /// </summary>
        /// <param name="token">The underlying token containing the value.</param>
        public Scalar(CXToken token)
        {
            Slot(Token = token);
        }
    }
}