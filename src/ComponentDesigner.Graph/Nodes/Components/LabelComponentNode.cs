using ComponentDesigner.Parser;

namespace ComponentDesigner.Nodes;

public sealed class LabelComponentNode : ComponentNode
{
    public override string Name => "label";

    public override ComponentTargetType Target => ComponentTargetType.Modal;

    public override IReadOnlyList<ComponentProperty> Properties { get; }

    public ComponentProperty Id { get; }
    public ComponentProperty Component { get; }
    public ComponentProperty Label { get; }
    public ComponentProperty Description { get; }

    public LabelComponentNode()
    {
        Properties =
        [
            Id = ComponentProperty.Id,
            Component = new(
                "component",
                kind: ComponentPropertyValueKind.Component,
                flags: ComponentPropertyFlags.FromChildren
            ),
            Label = new(
                "label",
                aliases: ["value"],
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

    public override ComponentState? CreateState(
        ComponentNodeInitializationContext context,
        IDiagnosticBag diagnostics,
        CancellationToken cancellationToken = default
    )
    {
        if (
            context.CXNode is not CXElement element ||
            base.CreateState(context, diagnostics, cancellationToken) is not { } state
        ) return null;


        /*
         * children of labels can be substituted into the 'component' and 'value' properties:
         *
         * <label>
         *      this text is the 'value'
         *      <button .../>
         * </label>
         */

        if (element.Children.Count is 0) return state;
        
        var childComponent = element.Children[element.Children.Count - 1];
        var labelLength = element.Children.Count;

        if (
            CXComponentGraph.IsLikelyComponent(
                context.GraphContext,
                childComponent,
                cancellationToken
            )
        )
        {
            state.SetPropertyValueToChildren(
                Component,
                context.AddChild(childComponent, cancellationToken)
            );

            labelLength--;
        }

        ComponentPropertyValue[] labelValues = element.Children
            .Take(labelLength)
            .SelectMany(GraphNodeEnumerator.GetNext)
            .OfType<CXValue>()
            .Select(x =>
                ComponentState.BuildPropertyValueFromSimpleSyntax(
                    context.GraphContext,
                    Label,
                    state.ChildSource,
                    x,
                    x.TextSpan,
                    cancellationToken
                )
            )
            .Where(x => x is not null)
            .ToArray()!;

        if (labelValues.Length > 0)
        {
            state.SetPropertyValue(
                Label,
                labelValues.Length switch
                {
                    1 => labelValues[0],
                    _ => new ComponentPropertyValue.Many(
                        state.ChildSource,
                        Label,
                        labelValues
                    )
                }
            );
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