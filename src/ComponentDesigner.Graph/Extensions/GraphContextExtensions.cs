using ComponentDesigner.Parser;

namespace ComponentDesigner;

public static class GraphContextExtensions
{
    extension(IGraphContext context)
    {
        public bool IsInterpolatedComponent(ICXNode? node, CancellationToken cancellationToken = default)
            => node switch
            {
                CXValue.Interpolation { InterpolationIndex: { } index } => context
                    .Renderer
                    .IsValidComponentType(
                        context,
                        context.GetInterpolationInfo(index).Symbol,
                        cancellationToken
                    ),
                CXToken { Kind: CXTokenKind.Interpolation, InterpolationIndex: { } index } => context
                    .Renderer
                    .IsValidComponentType(
                        context,
                        context.GetInterpolationInfo(index).Symbol,
                        cancellationToken
                    ),
                _ => false
            };
    }
}