using ComponentDesigner;
using ComponentDesigner.CSharp;
using ComponentDesigner.Nodes;

namespace Discord;

partial class DiscordNetRenderer
{
    public static Result<CSharpRender> RenderSeparator(
        IRenderContext<CSharpRender> context,
        SeparatorComponentNode separator,
        ComponentState state,
        CancellationToken cancellationToken
    ) => Construct(
        context,
        state,
        context.CompilationProvider.SeparatorBuilder,
        cancellationToken,
        ("id", separator.Id, CSharpValueGenerator.NullableInt32),
        ("spacing", separator.Spacing, CSharpValueGenerator.SeparatorSpacingSize),
        ("isDivider", separator.Divider, CSharpValueGenerator.NullableBoolean)
    );
}