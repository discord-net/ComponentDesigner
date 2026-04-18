using ComponentDesigner;
using ComponentDesigner.Nodes;

namespace Discord;

partial class DiscordNetRenderer
{
    public override Result<RenderedComponent> RenderCheckbox(
        IRendererContext context,
        CheckboxComponentNode checkbox,
        ComponentState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    ) => context.CompilationProvider
        .CheckboxBuilder(state.TextSpan, cancellationToken)
        .Combine(
            RenderPropertiesAsParameters(
                context, state, cancellationToken,
                ("customId", checkbox.CustomId, CSharpValueGenerator.String),
                ("defaultState", checkbox.Default, CSharpValueGenerator.NullableBoolean),
                ("id", checkbox.Id, CSharpValueGenerator.NullableInt32)
            ),
            (symbol, properties) => new RenderedComponent(
                $"new {symbol.ToQualifiedName()}({properties})",
                symbol
            )
        )
        .Map(ApplyRefParameter(context, state, cancellationToken))
        .Map(GetConverterFromOptions(context, state, typingContext, cancellationToken));

    public override Result<RenderedComponent> RenderCheckboxGroup(
        IRendererContext context,
        CheckboxGroupComponentNode checkboxGroup,
        ComponentState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    ) => context.CompilationProvider
        .CheckboxGroupBuilder(state.TextSpan, cancellationToken)
        .Combine(
            RenderPropertiesAsParameters(
                context, state, cancellationToken,
                ("customId", checkboxGroup.CustomId, CSharpValueGenerator.String),
                ("options", checkboxGroup.Options, new(RenderCheckboxGroupOptionProperty)),
                ("minValues", checkboxGroup.MinValues, CSharpValueGenerator.NullableInt32),
                ("maxValues", checkboxGroup.MaxValues, CSharpValueGenerator.NullableInt32),
                ("isRequired", checkboxGroup.Required, CSharpValueGenerator.NullableBoolean),
                ("id", checkboxGroup.Id, CSharpValueGenerator.NullableInt32)
            ),
            (symbol, properties) => new RenderedComponent(
                $"new {symbol.ToQualifiedName()}({properties})",
                symbol
            )
        )
        .Map(ApplyRefParameter(context, state, cancellationToken))
        .Map(GetConverterFromOptions(context, state, typingContext, cancellationToken));
    
    private static Result<string> RenderCheckboxGroupOptionProperty(
        IRendererContext context,
        ComponentPropertyValue propertyValue,
        CancellationToken cancellationToken
    )
    {
        return RenderGenericArrayOfValue(
            context,
            propertyValue,
            cancellationToken,
            componentHandler: RenderComponentValue,
            interpolationHandler: RenderInterpolation
        );

        static Result<string> RenderComponentValue(
            IRendererContext context,
            ComponentPropertyValue.Component component,
            CancellationToken cancellationToken
        ) => context
            .RenderGraphNode(
                component.GraphNode,
                cancellationToken: cancellationToken
            )
            .AsSource;

        static Result<string> RenderInterpolation(
            IRendererContext context,
            ComponentPropertyValue.Interpolation interpolation,
            CancellationToken cancellationToken
        )
        {
            if (interpolation.Info.Symbol is null)
                return Diagnostic
                    .TypeMismatch(
                        "unknown",
                        "CheckboxGroupOptionProperties"
                    )
                    .At(interpolation);

            if (interpolation.Info.Symbol.Equals(context.CompilationProvider.CheckboxGroupOptionProperties, cancellationToken))
                return context.GetReferenceToDesignerValue(
                    interpolation.Info,
                    interpolation.Info.Symbol
                );

            if (
                interpolation.Info.Symbol.Equals(
                    context.CompilationProvider.IEnumerableOf(context.CompilationProvider.CheckboxGroupOptionProperties),
                    cancellationToken
                )
            )
            {
                return $"..{context.GetReferenceToDesignerValue(
                    interpolation.Info,
                    interpolation.Info.Symbol
                )}";
            }

            return Diagnostic
                .TypeMismatch(
                    interpolation.Info.Symbol,
                    "CheckboxGroupOptionProperties"
                )
                .At(interpolation);
        }
    }
    
    public override Result<RenderedComponent> RenderCheckboxGroupOption(
        IRendererContext context,
        CheckboxGroupOptionComponentNode checkboxGroupOption,
        ComponentState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    ) => context.CompilationProvider
        .CheckboxGroupOptionProperties(state.TextSpan, cancellationToken)
        .Combine(
            RenderPropertiesAsParameters(
                context, state, cancellationToken,
                ("value", checkboxGroupOption.Value, CSharpValueGenerator.String),
                ("label", checkboxGroupOption.Label, CSharpValueGenerator.String),
                ("description", checkboxGroupOption.Description, CSharpValueGenerator.NullableString),
                ("defaultState", checkboxGroupOption.Default, CSharpValueGenerator.NullableBoolean)
            ),
            (symbol, properties) => new RenderedComponent(
                $"new {symbol.ToQualifiedName()}({properties})",
                symbol
            )
        )
        .Map(ApplyRefParameter(context, state, cancellationToken))
        .Map(GetConverterFromOptions(context, state, typingContext, cancellationToken));
}