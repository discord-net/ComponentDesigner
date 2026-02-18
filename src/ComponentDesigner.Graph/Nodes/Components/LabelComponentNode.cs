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
                "component"
            ),
            Value = new(
                "value"
            ),
            Description = new(
                "description",
                isOptional: true
            )
        ];
    }

    public override void RegisterGraphNode(
        ComponentGraphInitializationContext context,
        CancellationToken cancellationToken = default
    )
    {
        context.Push(
            this,
            cxNode: context.CXNode
        );
    }

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
            state.SetPropertyValue(Value, childValue);
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
        Validators.ValidateLabel,
        context.Renderer.RenderLabel,
        cancellationToken
    );
}