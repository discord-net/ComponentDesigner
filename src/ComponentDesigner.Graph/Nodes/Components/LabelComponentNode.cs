using ComponentDesigner.Parser;

namespace ComponentDesigner.Nodes;

public sealed class LabelComponentNode : ComponentNode
{
    public override string Name => "label";

    public override bool IsParentOfOtherComponents => true;

    public override IReadOnlyList<ComponentProperty> Properties { get; }

    public ComponentProperty Id { get; }
    public ComponentProperty Component { get; }
    public ComponentProperty Value { get; }
    public ComponentProperty Description { get; }

    public LabelComponentNode()
    {
        Properties =
        [
            Id = ComponentProperty.Id,
            Component = new(
                "component",
                kind: ComponentPropertyValueKind.Component
            ),
            Value = new(
                "value",
                kind: ComponentPropertyValueKind.SyntaxValue
            ),
            Description = new(
                "description",
                isOptional: true,
                kind: ComponentPropertyValueKind.SyntaxValue
            )
        ];
    }

    public override void RegisterGraphNode(
        ComponentGraphInitializationContext context,
        CancellationToken cancellationToken = default
    ) => base.RegisterGraphNode(context, includeElementChildren: false, cancellationToken);

    public override ComponentState? Initialize(
        ComponentNodeInitializationContext context,
        IDiagnosticBag diagnostics,
        CancellationToken cancellationToken = default
    )
    {
        if (
            context.CXNode is not CXElement element ||
            base.Initialize(context, diagnostics, cancellationToken) is not { } state
        ) return null;

        /*
         * children of labels can be substituted into the 'component' and 'value' properties:
         *
         * <label>
         *      this text is the 'value'
         *      <button .../>
         * </label>
         */

        CXValue? childValue = null;
        CXNode? childComponent = null;

        switch (element.Children.FirstOrDefault())
        {
            case CXValue value:
            {
                childValue = value;

                if (
                    element.Children.Count > 1 &&
                    CXComponentGraph.IsLikelyComponent(context.GraphContext, element.Children[0], cancellationToken)
                )
                {
                    childComponent = element.Children[1];
                }

                break;
            }

            case { } any when CXComponentGraph.IsLikelyComponent(context.GraphContext, any, cancellationToken):
                childComponent = any;
                break;
        }

        if (childComponent is not null)
        {
            context.AddChild(childComponent, cancellationToken);
            state.SetPropertyValueToChild(Component, childComponent);
        }

        if (childValue is not null)
        {
            state.SetPropertyValue(context, Value, childValue, cancellationToken);
        }

        return state;
    }

    public override void Validate(
        IComponentContext context, ComponentState state, IDiagnosticBag bag,
        CancellationToken cancellationToken = default
    ) => Validators.ValidateLabel(context, this, state, bag);

    public override Result<RenderedComponent> Render(
        ComponentEmitContext context,
        ComponentState state,
        ComponentOptions options,
        CancellationToken cancellationToken = default
    ) => context.Renderer.RenderLabel(context, this, state, options.TypingContext, cancellationToken);
}