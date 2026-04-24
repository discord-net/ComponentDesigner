using ComponentDesigner;
using ComponentDesigner.CSharp;
using ComponentDesigner.Nodes;

namespace Discord;

partial class DiscordNetRenderer
{
    public static Result<CSharpRender> RenderLabel(
        IRenderContext<CSharpRender> context,
        LabelComponentNode label,
        ComponentState state,
        CancellationToken cancellationToken
    ) => Construct(
        context,
        state,
        context.CompilationProvider.LabelBuilder,
        cancellationToken,
        ("id", label.Id, CSharpValueGenerator.NullableInt32),
        ("label", label.Label, CSharpValueGenerator.String),
        ("description", label.Description, CSharpValueGenerator.NullableString),
        ("component", label.Component, IMessageComponentBuilder)
    );
}