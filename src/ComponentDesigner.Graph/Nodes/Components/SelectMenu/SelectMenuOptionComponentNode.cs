using ComponentDesigner.Parser;

namespace ComponentDesigner.Nodes;

public sealed class SelectMenuOptionComponentNode : ComponentNode
{
    public override string Name => "select-menu-option";

    public override IReadOnlyList<string> Aliases { get; } = ["option"];

    public override bool AllowChildrenInCX => true;

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
            Label = new("label"),
            Value = new("value"),
            Description = new("description", isOptional: true),
            Emoji = new("emoji", isOptional: true),
            IsDefault = new("default", isOptional: true, requiresValue: false)
        ];
    }
    
    public override ComponentState? Initialize(
        ComponentNodeInitializationContext context,
        IDiagnosticBag diagnostics,
        CancellationToken cancellationToken = default
    )
    {
        if (base.Initialize(context, diagnostics, cancellationToken) is not { } state) return null;

        // check for substitute of label or value
        var label = state.GetPropertyValue(Label);
        var value = state.GetPropertyValue(Value);

        if (
            (label.IsSpecified && value.IsSpecified) ||
            (!label.IsSpecified && !value.IsSpecified) ||
            context.CXNode is not CXElement element
        ) return state;

        if (element.Children.Count > 0 && element.Children[0] is CXValue childValue)
        {
            var propertyToSet = label.IsSpecified ? Value : Label;

            state.SetPropertyValue(propertyToSet, childValue);
        }

        return state;
    }

    public override Result<RenderedComponent> Emit(
        ComponentState state,
        ComponentEmitContext context,
        ComponentOptions options,
        CancellationToken cancellationToken = default
    ) => ValidateAndRender(
        this,
        state,
        context,
        options,
        Validators.ValidateSelectMenuOption,
        context.Renderer.RenderSelectMenuOption,
        cancellationToken
    );
}