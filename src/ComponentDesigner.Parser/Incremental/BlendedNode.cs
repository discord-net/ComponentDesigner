namespace ComponentDesigner.Parser;

/// <summary>
///     Represents a node blended from a <see cref="CXBlender"/>.
/// </summary>
/// <param name="Cursor">The cursor describing the source/ast state of the blended node.</param>
/// <param name="ASTNode">The AST node returned from the blender.</param>
/// <param name="Reused">Whether the node was reused.</param>
public readonly record struct BlendedNode(
    Cursor Cursor,
    ICXNode ASTNode,
    bool Reused
)
{
    /// <summary>
    ///     Clones the inner <see cref="ICXNode"/> if it was reused and returns a copy of the current
    ///     <see cref="BlendedNode"/> with the new AST node.
    /// </summary>
    /// <returns>A <see cref="BlendedNode"/> with the updated inner AST node.</returns>
    public BlendedNode WithClonedASTNode()
    {
        if (!Reused) return this;

        return this with
        {
            ASTNode = (ICXNode)ASTNode.Clone()
        };
    }
}