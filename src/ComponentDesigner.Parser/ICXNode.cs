using System;
using System.Collections.Generic;
using ComponentDesigner;

namespace ComponentDesigner.Parser;

/// <summary>
///     Represents any AST node within a syntax tree, including terminal nodes.
/// </summary>
public interface ICXNode : IEquatable<ICXNode>, ICloneable, ISourceLocatable
{
    /// <summary>
    ///     Gets the full span, including trivia, that this node was parsed from in the source.
    /// </summary>
    CXTextSpan FullTextSpan { get; }
    
    /// <summary>
    ///     Gets the full width in characters of this node.
    /// </summary>
    int Width { get; }

    /// <summary>
    ///     Gets the width of this node in the AST.
    /// </summary>
    /// <remarks>
    ///     A terminal node always has a width of zero.
    /// </remarks>
    int GraphWidth { get; }

    /// <summary>
    ///     Gets whether this node contains any <see cref="CXDiagnostic"/> with an error severity. 
    /// </summary>
    bool HasErrors { get; }
    
    /// <summary>
    ///     Gets or sets the non-terminal parent of this node.
    /// </summary>
    CXNode? Parent { get; internal set; }

    /// <summary>
    ///     Gets a collection of slots representing child AST nodes of this node.
    /// </summary>
    /// <remarks>
    ///     Terminal nodes always return an empty collection.
    /// </remarks>
    IReadOnlyList<ICXNode> Slots { get; }
    
    /// <summary>
    ///     Gets the <see cref="CXDocument"/> this node belongs to.
    /// </summary>
    /// <remarks>
    ///     During the parsing stage, where the entire document hasn't been fully parsed, this property will return
    ///     <see langword="null"/>.
    /// </remarks>
    CXDocument? Document { get; }

    /// <summary>
    ///     Gets the lexed leading trivia belonging to this node.
    /// </summary>
    LexedCXTrivia LeadingTrivia { get; }
    
    /// <summary>
    ///     Gets the lexed trailing trivia belonging to this node.
    /// </summary>
    LexedCXTrivia TrailingTrivia { get; }
    
    /// <summary>
    ///     Gets a read-only list of diagnostic descriptors relating to this node.
    /// </summary>
    internal IReadOnlyList<CXDiagnosticDescriptor> DiagnosticDescriptors { get; init; }

    internal CXDiagnostic CreateDiagnostic(CXDiagnosticDescriptor descriptor);
    
    /// <summary>
    ///     Resets any computations that this node has cached.
    /// </summary>
    void ResetCachedState();

    /// <summary>
    ///     Converts this node into its equivalent CX syntax. 
    /// </summary>
    /// <param name="includeLeadingTrivia">
    ///     Whether to include <see cref="LeadingTrivia"/> in the syntax.
    /// </param>
    /// <param name="includeTrailingTrivia">
    ///     Whether to include <see cref="TrailingTrivia"/> in the syntax.
    /// </param>
    /// <returns></returns>
    string ToString(bool includeLeadingTrivia, bool includeTrailingTrivia);
}
