using ComponentDesigner;
using ComponentDesigner.Nodes;

namespace Discord;

partial class DiscordNetRenderer
{
    public override Result<RenderedComponent> RenderRadioGroup(
        IRendererContext context,
        RadioGroupComponentNode radioGroup,
        ComponentState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    ) => context.CompilationProvider
        .RadioGroupBuilder(state.TextSpan, cancellationToken)
        .Combine(
            RenderPropertiesAsParameters(
                context, state, cancellationToken,
                ("customId", radioGroup.CustomId, CSharpValueGenerator.String),
                ("options", radioGroup.Options, new(RenderRadioGroupOptionProperty)),
                ("isRequired", radioGroup.Required, CSharpValueGenerator.NullableBoolean),
                ("id", radioGroup.Id, CSharpValueGenerator.NullableInt32)
            ),
            (symbol, properties) => new RenderedComponent(
                $"new {symbol.ToQualifiedName()}({properties})",
                symbol
            )
        )
        .Map(ApplyRefParameter(context, state, cancellationToken))
        .Map(GetConverterFromOptions(context, state, typingContext, cancellationToken));

    private static Result<string> RenderRadioGroupOptionProperty(
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
                        "RadioGroupOptionProperties"
                    )
                    .At(interpolation);

            if (interpolation.Info.Symbol.Equals(context.CompilationProvider.RadioGroupOptionProperties, cancellationToken))
                return context.GetReferenceToDesignerValue(
                    interpolation.Info,
                    interpolation.Info.Symbol
                );

            if (
                interpolation.Info.Symbol.Equals(
                    context.CompilationProvider.IEnumerableOf(context.CompilationProvider.RadioGroupOptionProperties),
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
                    "RadioGroupOptionProperties"
                )
                .At(interpolation);
        }
    }
    
    public override Result<RenderedComponent> RenderRadioGroupOption(
        IRendererContext context,
        RadioGroupOptionComponentNode radioGroupOption,
        ComponentState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    ) =>  context.CompilationProvider
        .RadioGroupOptionProperties(state.TextSpan, cancellationToken)
        .Combine(
            RenderPropertiesAsParameters(
                context, state, cancellationToken,
                ("value", radioGroupOption.Value, CSharpValueGenerator.String),
                ("label", radioGroupOption.Label, CSharpValueGenerator.String),
                ("description", radioGroupOption.Description, CSharpValueGenerator.NullableString),
                ("isDefault", radioGroupOption.Default, CSharpValueGenerator.NullableBoolean)
            ),
            (symbol, properties) => new RenderedComponent(
                $"new {symbol.ToQualifiedName()}({properties})",
                symbol
            )
        )
        .Map(ApplyRefParameter(context, state, cancellationToken))
        .Map(GetConverterFromOptions(context, state, typingContext, cancellationToken));
}