using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Discord.CX.Parser;

/// <summary>
///     Contains <see cref="ICXNode"/> related extensions.
/// </summary>
public static class CXNodeExtensions
{
    extension(IReadOnlyList<CXToken> tokens)
    {
        public string ToValueString(bool includeLeadingTrivia = false, bool includeTrailingTrivia = false)
        {
            if (tokens.Count is 0) return string.Empty;
            
            var sb = new StringBuilder();

            for (var i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];
                var isFirst = i == 0;
                var isLast = i == tokens.Count - 1;

                if (includeLeadingTrivia || !isFirst)
                    sb.Append(token.LeadingTrivia);

                sb.Append(token.Value);

                if (includeTrailingTrivia || !isLast)
                    sb.Append(token.TrailingTrivia);
            }

            return sb.ToString();
        }
    }
    
    extension(ICXNode node)
    {
        /// <summary>
        ///     Gets whether this node or any descending nodes contains diagnostics.
        /// </summary>
        public bool HasDiagnostics
        {
            get
            {
                if (node.DiagnosticDescriptors.Count is not 0) return true;

                return node.Slots.Any(x => x.HasDiagnostics);
            }
        }

        /// <summary>
        ///     Gets whether this node contains any tokens that have the missing flag specified.
        /// </summary>
        public bool HasMissingTokens
        {
            get
            {
                if (node is CXToken token) return token.IsMissing;

                return node.Slots.Any(x => x.HasMissingTokens);
            }
        }
        
        /// <summary>
        ///     Gets the diagnostics reported for this node.
        /// </summary>
        public IReadOnlyList<CXDiagnostic> Diagnostics
            => [..node.DiagnosticDescriptors.Select(node.CreateDiagnostic)];

        /// <summary>
        ///     Gets all diagnostics from this node and any descending nodes.
        /// </summary>
        public IReadOnlyList<CXDiagnostic> AllDiagnostics
            => [..node.Diagnostics, ..node.Slots.SelectMany(x => x.AllDiagnostics)];
        
        /// <summary>
        ///     Gets the parser used to parse this <see cref="ICXNode"/>.
        /// </summary>
        /// <remarks>
        ///     During the parsing stage, where the entire document hasn't been fully parsed,
        ///     this property will return <see langword="null"/>.
        /// </remarks>
        public CXParser? Parser
        {
            get
            {
                if (node is CXDocument document) return document.Parser;
                
                return node.Document?.Parser;
            }
        }
        
        /// <summary>
        ///     Gets the source text containing this AST node.
        /// </summary>
        /// <remarks>
        ///     During the parsing stage, where the entire document hasn't been fully parsed,
        ///     this property will return <see langword="null"/>.
        /// </remarks>
        public CXSourceText? Source => node.Parser?.Reader.Source;

        /// <summary>
        ///     Gets the character offset of this node relative to the entire AST. 
        /// </summary>
        public int Offset
        {
            get
            {
                /*
                 * Traverse the AST upwards until we hit the root node, on each step up, add up the left-most
                 * siblings widths
                 */
                var offset = 0;
                var current = node;

                while (current is not null)
                {
                    if (current.Parent is null) return offset;
                
                    var index = current.GetParentSlotIndex();

                    // continue traversing if we are the left-most sibling
                    if (index is 0)
                    {
                        current = current.Parent;
                        continue;
                    }

                    // add up the widths of the left-most siblings
                    for (var i = 0; i < index; i++)
                        offset += current.Parent.Slots[i].Width;

                    current = current.Parent;
                }

                return offset;
            }
        }
        
        
        /// <summary>
        ///     Gets the index of this node within its parents <see cref="ICXNode.Slots"/>.
        /// </summary>
        /// <returns>
        ///     The index of this node within its parent; <c>-1</c> if there is no parent or this node isn't in its parents'
        ///     slots.
        /// </returns>
        public int GetParentSlotIndex()
        {
            if (node.Parent is null) return -1;

            for (var i = 0; i < node.Parent.Slots.Count; i++)
                if (ReferenceEquals(node, node.Parent.Slots[i]))
                    return i;

            return -1;
        }
        
        /// <summary>
        ///     Converts this <see cref="ICXNode"/> into its equivalent CX syntax including trivia.
        /// </summary>
        /// <returns>The CX syntax representing this <see cref="ICXNode"/>.</returns>
        public string ToFullString() => node.ToString(true, true);

    }
}