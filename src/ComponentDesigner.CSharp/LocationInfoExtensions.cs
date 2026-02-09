using Microsoft.CodeAnalysis;

namespace ComponentDesigner.CSharp;

public static class LocationInfoExtensions
{
    extension(LocationInfo)
    {
        public static LocationInfo From(SyntaxNode node, CancellationToken cancellationToken = default)
            => new(
                node.SyntaxTree.FilePath,
                node.Span.AsCXTextSpan,
                LinePositionSpan.From(node.SyntaxTree.GetLineSpan(node.Span, cancellationToken))
            );

        public static LocationInfo From(Location location, CancellationToken cancellationToken = default)
            => new(
                location.SourceTree?.FilePath,
                location.SourceSpan.AsCXTextSpan,
                location.SourceTree is null
                    ? default
                    : LinePositionSpan.From(location.SourceTree.GetLineSpan(location.SourceSpan, cancellationToken))
            );
    }
}