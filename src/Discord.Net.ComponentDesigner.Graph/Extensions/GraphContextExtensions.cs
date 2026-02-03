using Discord.CX.Parser;

namespace Discord.CX;

public static class GraphContextExtensions
{
    extension(IGraphContext context)
    {
        public bool IsInterpolatedComponent(ICXNode? node, CancellationToken token = default)
            => node switch
            {
                CXValue.Interpolation { InterpolationIndex: { } index } => context
                    .Renderer
                    .IsValidComponentType(
                        context,
                        context.GetInterpolationInfo(index).Symbol,
                        token
                    ),
                CXToken { Kind: CXTokenKind.Interpolation, InterpolationIndex: { } index } => context
                    .Renderer
                    .IsValidComponentType(
                        context,
                        context.GetInterpolationInfo(index).Symbol,
                        token
                    ),
                _ => false
            };
    }
}