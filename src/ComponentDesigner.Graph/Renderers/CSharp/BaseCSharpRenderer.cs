using System.Text;
using ComponentDesigner.Nodes;
using ComponentDesigner.Parser;

namespace ComponentDesigner.CSharp;

public abstract class BaseCSharpRenderer : IComponentRenderer
{
    public abstract string Name { get; }

    protected virtual CSharpValueGenerator? GetCustomGeneratorForSymbol(
        ICompilationProvider compilationProvider,
        ICSharpTypeSymbol symbol
    ) => null;

    private CSharpValueGenerator GetGeneratorForSymbol(
        ICompilationProvider compilationProvider,
        ICSharpTypeSymbol symbol
    ) => GetCustomGeneratorForSymbol(compilationProvider, symbol) ??
         CSharpValueGenerator.FromSymbol(compilationProvider, symbol);

    public virtual Result<RenderedComponent> RenderFunctionalComponent(
        IRendererContext context,
        FunctionalComponentNode functionalComponent,
        FunctionalState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    )
    {
        var bag = DiagnosticBag.Get();

        using var _ = ObjectPool<StringBuilder>.GetScoped(out var parameters);
        parameters.Clear();

        for (var i = 0; i < state.Parameters.Count; i++)
        {
            var parameter = state.Parameters[i];
            var parameterSymbol = state.Symbol.Parameters[i];

            // can we omit?
            var parameterValue = state.GetPropertyValue(parameter);

            if (!parameterValue.HasValue)
            {
                if (parameter.IsOptional)
                {
                    bag.AddDiagnostics(
                        state.ElementIdentifierTextSpanOrBetter.Report(
                            Diagnostic.RequiredPropertyNotSpecified(functionalComponent, parameter)
                        )
                    );
                }

                continue;
            }

            switch (parameterValue)
            {
                case ComponentPropertyValue.AttributeElement attributeElement:
                {
                    var result = context.RenderGraphNode(
                        attributeElement.GraphNode,
                        new(TypingContext: new(parameterSymbol.Type)),
                        cancellationToken
                    );

                    bag.AddDiagnostics(result.Diagnostics);

                    if (result.HasValue) AppendParameter(parameters, parameter.Name, result.Value.Source);

                    break;
                }
                case ComponentPropertyValue.AttributeValue attributeValue:
                {
                    var generator = GetGeneratorForSymbol(
                        context.CompilationProvider,
                        parameterSymbol.Type
                    );

                    var result = generator.Render(context, attributeValue, cancellationToken: cancellationToken);

                    bag.AddDiagnostics(result.Diagnostics);

                    if (result.HasValue) AppendParameter(parameters, parameter.Name, result.Value);

                    break;
                }

                case ComponentPropertyValue.Children children:
                {
                    // TODO: figure out collection conversion and builder conversion etc
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(parameterValue));
            }
        }

        if (bag.HasErrors) return new(bag.Use());

        if (parameters.Length > 0)
        {
            parameters.Insert(0, Environment.NewLine).AppendLine();
        }

        return new(
            $"{MakeMethodReference(state.CXNode, context, state.Symbol)}({parameters})"
        );

        static void AppendParameter(StringBuilder builder, string name, string value)
        {
            if (builder.Length > 0) builder.AppendLine(",");
            builder.Append(name).Append(": ").Append(value);
        }

        static string MakeMethodReference(CXElement element, IComponentContext context, ICSharpMethodSymbol symbol)
        {
            switch (element.OpeningTag.Identifier)
            {
                case CXIdentifier.Simple:
                    return $"{symbol.ContainingType.ToQualifiedName()}.{symbol.Name}";
                case CXIdentifier.Interpolated { InterpolationToken: { } token }:
                    var info = context.GetInterpolationInfo(token);

                    return $"{context.GetReferenceToDesignerValue(info)}.{symbol.Name}";

                default: throw new ArgumentOutOfRangeException(nameof(element));
            }
        }
    }

    public virtual Result<RenderedComponent> RenderInterpolation(
        IRendererContext context,
        IInterpolationInfo info,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    )
    {
        return new RenderedComponent(
            context.GetReferenceToDesignerValue(info, info.Symbol),
            info.Symbol
        );
    }
    
    public abstract bool IsValidComponentType(
        IComponentContext context,
        ICSharpTypeSymbol? symbol,
        CancellationToken cancellationToken = default
    );

    public abstract Result<string> RenderComponents(
        CXComponentGraph graph,
        ComponentEmitContext context,
        CancellationToken cancellationToken = default
    );

    public abstract Result<RenderedComponent> RenderThumbnail(
        IRendererContext context,
        ThumbnailComponentNode thumbnail,
        ComponentState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    );

    public abstract Result<RenderedComponent> RenderTextInput(
        IRendererContext context,
        TextInputComponentNode textInput,
        ComponentState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    );

    public abstract Result<RenderedComponent> RenderSeparator(
        IRendererContext context,
        SeparatorComponentNode separator,
        ComponentState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    );

    public abstract Result<RenderedComponent> RenderSection(
        IRendererContext context,
        SectionComponentNode section,
        ComponentState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    );

    public abstract Result<RenderedComponent> RenderLabel(
        IRendererContext context,
        LabelComponentNode label,
        ComponentState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    );

    public abstract Result<RenderedComponent> RenderFile(
        IRendererContext context,
        FileComponentNode file,
        ComponentState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    );

    public abstract Result<RenderedComponent> RenderFileUpload(
        IRendererContext context,
        FileUploadComponentNode fileUpload,
        ComponentState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    );

    public abstract Result<RenderedComponent> RenderButton(
        IRendererContext context,
        ButtonComponentNode button,
        ButtonState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    );

    public abstract Result<RenderedComponent> RenderActionRow(
        IRendererContext context,
        ActionRowComponentNode actionRow,
        ComponentState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    );

    public abstract Result<RenderedComponent> RenderContainer(
        IRendererContext context,
        ContainerComponentNode container,
        ComponentState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    );

    public abstract Result<RenderedComponent> RenderTextDisplay(
        IRendererContext context,
        TextDisplayComponentNode textDisplay,
        TextDisplayState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    );
}