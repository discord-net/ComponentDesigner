using System.Text;
using ComponentDesigner.Nodes;
using ComponentDesigner.Nodes.TextControls;
using ComponentDesigner.Parser;
using ComponentDesigner.Util;

namespace ComponentDesigner.CSharp;

public abstract class BaseCSharpRenderer : IComponentRenderer
{
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
        var bag = PooledDiagnosticBag.Get();

        using var _ = StringBuilder.Pooled(out var parameters);

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
                    bag.Add(
                        state.ElementIdentifierTextSpanOrBetter.Report(
                            Diagnostic.RequiredPropertyNotSpecified(functionalComponent, parameter)
                        )
                    );
                }

                continue;
            }

            switch (parameterValue)
            {
                case ComponentPropertyValue.AttributeComponent attributeElement:
                {
                    var result = context.RenderGraphNode(
                        attributeElement.GraphNode,
                        new(TypingContext: new(parameterSymbol.Type)),
                        cancellationToken
                    );

                    bag.Add(result.Diagnostics);

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

                    bag.Add(result.Diagnostics);

                    if (result.HasValue) AppendParameter(parameters, parameter.Name, result.Value);

                    break;
                }

                case ComponentPropertyValue.Component children:
                {
                    // TODO: figure out collection conversion and builder conversion etc
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(parameterValue));
            }
        }

        if (bag.HasErrors) return new(bag.ToCollection());

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

                    return $"{context.GetReferenceToDesignerValue(info, info.Symbol)}.{symbol.Name}";

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

    public virtual Result<RenderedComponent> RenderTextControls(
        IRendererContext context,
        TextControlGraph textControlGraph,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    )
    {
        var startInterpolation = textControlGraph.ContainsInterpolations
            ? new string('{', textControlGraph.InterpolationDollarSignRequirement)
            : string.Empty;

        var endInterpolation = textControlGraph.ContainsInterpolations
            ? new string('}', textControlGraph.InterpolationDollarSignRequirement)
            : string.Empty;

        var options = new TextControlOptions(
            startInterpolation,
            endInterpolation
        );

        return textControlGraph
            .RootElements
            .Select(x => x.Render(context, options, cancellationToken))
            .FlattenAll()
            .Map(Join)
            .Map(x => x with
            {
                LeadingTrivia = x.LeadingTrivia.TrimLeadingSyntaxIndentation(),
                TrailingTrivia = x.TrailingTrivia.TrimTrailingSyntaxIndentation()
            })
            .Map(control =>
                ToCSharpString(
                    control,
                    textControlGraph.ContainsInterpolations,
                    textControlGraph.InterpolationDollarSignRequirement
                )
            )
            .Map(source => new RenderedComponent(source, context.CompilationProvider.String));

        static Result<string> ToCSharpString(
            TextControl control,
            bool hasInterpolations,
            int interpolationDollarCount
        )
        {
            var quoteCount = (StringGenerator.GetSequentialQuoteCount(control.Value) + 1) switch
            {
                2 => 3,
                var r => r
            };

            var isMultiline = control.ContainsNewLines || quoteCount > 1;
            var isMultilineInterpolation = isMultiline && hasInterpolations;

            if (isMultiline)
                quoteCount = Math.Max(3, quoteCount);

            var dollars = hasInterpolations
                ? new string(
                    '$',
                    interpolationDollarCount
                )
                : string.Empty;

            var quotes = new string('"', quoteCount);

            var pad = isMultilineInterpolation
                ? new string(' ', interpolationDollarCount)
                : string.Empty;

            using var _ = StringBuilder.Pooled(out var sb);

            // start on newline if it's a multi-line string
            if (isMultiline) sb.AppendLine();

            sb.Append(dollars).Append(quotes);

            if (isMultiline) sb.AppendLine();

            var value = control.ToString().NormalizeIndentation().Trim(['\r', '\n']);

            if (isMultilineInterpolation)
                value = value.Indent(interpolationDollarCount);

            sb.Append(value);

            if (isMultiline) sb.AppendLine();

            if (isMultilineInterpolation) sb.Append(pad);
            sb.Append(quotes);

            return sb.ToString();
        }

        static TextControl Join(EquatableArray<TextControl> elements)
        {
            if (elements.Count is 0) return TextControl.Empty;

            var sb = new StringBuilder();
            var containsNewLines = false;

            for (var i = 0; i < elements.Count; i++)
            {
                var render = elements[i];

                if (i is not 0)
                {
                    sb.Append(render.LeadingTrivia);
                    containsNewLines |= render.LeadingTrivia.ContainsNewlines;
                }

                sb.Append(render.Value);
                containsNewLines |= render.ValueContainsNewLines;

                if (i < elements.Count - 1)
                {
                    sb.Append(render.TrailingTrivia);
                    containsNewLines |= render.TrailingTrivia.ContainsNewlines;
                }
            }

            return new TextControl(
                elements[0].LeadingTrivia,
                elements[elements.Count - 1].TrailingTrivia,
                sb.ToString(),
                containsNewLines
            );
        }
    }

    public abstract Result<string> RenderComponents(
        CXComponentGraph graph,
        ComponentEmitContext context,
        CancellationToken cancellationToken = default
    );

    public abstract Result<RenderedComponent> RenderMediaGalleryItem(
        IRendererContext context,
        MediaGalleryItemComponentNode mediaGalleryItem,
        ComponentState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    );

    public abstract Result<RenderedComponent> RenderMediaGallery(
        IRendererContext context,
        MediaGalleryComponentNode mediaGallery,
        ComponentState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    );

    public abstract Result<RenderedComponent> RenderSelectMenu(
        IRendererContext context,
        SelectMenuComponentNode selectMenu,
        SelectMenuState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    );

    public abstract Result<RenderedComponent> RenderSelectMenuOption(
        IRendererContext context,
        SelectMenuOptionComponentNode option,
        ComponentState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    );

    public abstract Result<RenderedComponent> RenderSelectMenuDefaultValue(
        IRendererContext context,
        SelectMenuDefaultValueComponentNode option,
        DefaultValueState state,
        RendererTypingContext? typingContext = null,
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