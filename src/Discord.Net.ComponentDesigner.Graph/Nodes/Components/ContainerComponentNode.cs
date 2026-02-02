using Discord.CX.Parser;

namespace Discord.CX.Nodes;

public sealed class ContainerComponentNode : ComponentNode
{
    public override string Name => "container";

    public override IReadOnlyList<ComponentProperty> Properties { get; }

    public ComponentProperty Id { get; }
    public ComponentProperty AccentColor { get; }
    public ComponentProperty IsSpoiler { get; }

    public ContainerComponentNode()
    {
        Properties =
        [
            Id = ComponentProperty.Id,
            AccentColor = new(
                name: "accentColor",
                isOptional: true,
                aliases: ["color", "accent"]
            ),
            IsSpoiler = new(
                name: "spoiler",
                isOptional: true,
                requiresValue: false
            )
        ];
    }

    public override Result<string> Emit(
        ComponentState state,
        ComponentEmitContext context,
        ComponentOptions options,
        CancellationToken token = default
    )
    {
        var bag = DiagnosticBag.Get();

        Validate(state, bag);
        
        foreach (var child in state.Children)
            ValidateChildIsAllowedInContainer(state, bag, child.Component);

        return context.Renderer
            .Render(context, this, state, token)
            .AddDiagnostics(bag);
    }

    private void ValidateChildIsAllowedInContainer(ComponentState state, IDiagnosticBag bag, IComponentNode child)
    {
        // TODO: rest of components
        if (
            child is not IDynamicComponentNode
        )
        {
            bag.AddDiagnostics(
                state.TextSpan.Report(
                    Diagnostic.InvalidChildOfComponent(this, child)
                )
            );
        }
    }
}