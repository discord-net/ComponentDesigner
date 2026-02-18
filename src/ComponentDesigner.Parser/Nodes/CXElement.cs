using ComponentDesigner;

namespace ComponentDesigner.Parser;

/// <summary>
///     A data type containing the AST nodes related to an opening tag within a <see cref="CXElement"/>.
/// </summary>
/// <param name="StartToken">The opening tags starting token.</param>
/// <param name="Identifier">The opening tags identifier.</param>
/// <param name="Attributes">The opening tags' attributes.</param>
/// <param name="EndToken">The opening tags ending token.</param>
public readonly record struct CXElementOpeningTag(
    CXToken StartToken,
    CXIdentifier? Identifier,
    CXCollection<CXAttribute> Attributes,
    CXToken EndToken
);

/// <summary>
///     A data type containing the AST nodes related to a closing tag within a <see cref="CXElement"/>.
/// </summary>
/// <param name="StartToken">The closing tags starting token.</param>
/// <param name="IdentifierToken">The closing tags identifier token.</param>
/// <param name="EndToken">The closing tags ending token.</param>
public readonly record struct CXElementClosingTag(
    CXToken? StartToken,
    CXToken? IdentifierToken,
    CXToken? EndToken
)
{
    /// <summary>
    ///     Gets whether the closing tag was specified.
    /// </summary>
    public bool IsSpecified => StartToken is not null && EndToken is not null;
}

/// <summary>
///     An AST node representing a single element within the CX language.
/// </summary>
public sealed class CXElement : CXNode
{
    /// <summary>
    ///     Gets whether this node is a fragment (<c>&lt;&gt;</c>) element.
    /// </summary>
    public bool IsFragment => OpeningTag.Identifier is null && ClosingTag.IdentifierToken is null;
    
    /// <summary>
    ///     Gets the identifier of this element.
    /// </summary>
    /// <remarks>
    ///     If this element is a fragment, <see cref="string.Empty"/> is returned.
    /// </remarks>
    public string Identifier => OpeningTag.Identifier?.Value ?? string.Empty;

    /// <summary>
    ///     Gets the opening tag of this <see cref="CXElement"/> .
    /// </summary>
    public CXElementOpeningTag OpeningTag { get; }

    /// <summary>
    ///     Gets the opening tags' attributes.
    /// </summary>
    public CXCollection<CXAttribute> Attributes => OpeningTag.Attributes;
    
    /// <summary>
    ///     Gets the children of this <see cref="CXElement"/>.
    /// </summary>
    public CXCollection<CXNode> Children { get; }
    
    /// <summary>
    ///     Gets the closing tag of this <see cref="CXElement"/>.
    /// </summary>
    public CXElementClosingTag ClosingTag { get; }
    
    /// <summary>
    ///     Constructs a new <see cref="CXElement"/>
    /// </summary>
    /// <param name="openingTagStartToken">The opening tags starting token.</param>
    /// <param name="openingTagIdentifier">The opening tags identifier.</param>
    /// <param name="attributes">The attributes found in the opening tag.</param>
    /// <param name="openingTagEndToken">The opening tags ending token.</param>
    /// <param name="children">The children of this <see cref="CXElement"/>.</param>
    /// <param name="closingTagStartToken">The closing tags starting token.</param>
    /// <param name="closingTagIdentifierToken">The closing tags identifier token.</param>
    /// <param name="closingTagEndToken">The closing tags ending token.</param>
    public CXElement(
        CXToken openingTagStartToken,
        CXIdentifier? openingTagIdentifier,
        CXCollection<CXAttribute> attributes,
        CXToken openingTagEndToken,
        CXCollection<CXNode> children,
        CXToken? closingTagStartToken = null,
        CXToken? closingTagIdentifierToken = null,
        CXToken? closingTagEndToken = null
    )
    {
        OpeningTag = new(
            Slot(openingTagStartToken),
            Slot(openingTagIdentifier),
            Slot(attributes),
            Slot(openingTagEndToken)
        );
        
        Children = Slot(children);
        
        ClosingTag = new(
            Slot(closingTagStartToken),
            Slot(closingTagIdentifierToken),
            Slot(closingTagEndToken)
        );
    }

    protected internal override CXDiagnostic CreateDiagnostic(CXDiagnosticDescriptor descriptor)
    {
        if (descriptor.Code is CXErrorCode.MissingElementClosingTag)
        {
            var span = OpeningTag.Identifier?.TextSpan ?? CXTextSpan.FromBounds(
                OpeningTag.StartToken.TextSpan.Start,
                OpeningTag.EndToken.TextSpan.End
            );

            return new(descriptor, span);
        }

        return base.CreateDiagnostic(descriptor);
    }
}
