using ComponentDesigner.Parser;

namespace ComponentDesigner.Nodes;

public sealed class SelectMenuOptionComponentNode : ComponentNode
{
    public override string Name => "select-menu-option";

    public override IReadOnlyList<ComponentProperty> Properties { get; }

    public ComponentProperty Label { get; }
    public ComponentProperty Value { get; }
    public ComponentProperty Description { get; }
    public ComponentProperty Emoji { get; }
    public ComponentProperty IsDefault { get; }

    public SelectMenuOptionComponentNode()
    {
        Properties =
        [
            Label = new(
                "label",
                kind: ComponentPropertyValueKind.SyntaxValue
            ),
            Value = new(
                "value",
                kind: ComponentPropertyValueKind.SyntaxValue
            ),
            Description = new(
                "description",
                isOptional: true,
                kind: ComponentPropertyValueKind.SyntaxValue
            ),
            Emoji = new(
                "emoji",
                aliases: ["emote"],
                isOptional: true,
                kind: ComponentPropertyValueKind.SyntaxValue
            ),
            IsDefault = new(
                "default",
                isOptional: true,
                requiresValue: false,
                kind: ComponentPropertyValueKind.SyntaxValue
            )
        ];
    }

    public override ComponentState? CreateState(
        ComponentNodeInitializationContext context,
        IDiagnosticBag diagnostics,
        CancellationToken cancellationToken = default
    )
    {
        if (base.CreateState(context, diagnostics, cancellationToken) is not { } state) return null;

        // check for substitute of label or value
        var label = state.GetPropertyValue(Label);
        var value = state.GetPropertyValue(Value);

        if (
            label.IsNone == value.IsNone ||
            context.CXNode is not CXElement element
        ) return state;

        if (element.Children.Count > 0 && element.Children[0] is CXValue childValue)
        {
            var propertyToSet = label.IsSome ? Value : Label;

            state.SetPropertyValue(context, propertyToSet, childValue, cancellationToken);
        }

        return state;
    }

    public override void Validate(
        IComponentContext context, ComponentState state, IDiagnosticBag bag,
        CancellationToken cancellationToken = default
    ) => Validators.ValidateSelectMenuOption(context, this, state, bag);
}