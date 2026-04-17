using System.Text;
using ComponentDesigner.Nodes;
using ComponentDesigner.Nodes.TextControls;
using ComponentDesigner.Parser;
using ComponentDesigner.Util;

namespace ComponentDesigner.CSharp;

public abstract partial class BaseCSharpRenderer : IComponentRenderer
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

    protected static Func<RenderedComponent, Result<RenderedComponent>> GetConverterFromOptions<T>(
        IRendererContext context,
        T source,
        RendererTypingContext? typingContext,
        CancellationToken cancellationToken
    ) where T : ISourceLocatable
    {
        if (context.ComponentTypingProvider is null || typingContext is null)
            return static x => x;

        var targetSymbol = typingContext.Value.ConformingType;

        return render =>
        {
            if (render.Type is null) return render;

            return context.ComponentTypingProvider
                .Convert(
                    context,
                    render.Source.SourcedAt(source),
                    render.Type,
                    targetSymbol,
                    cancellationToken
                )
                .Map(x => new RenderedComponent(
                    x,
                    targetSymbol
                ));
        };
    }

    public virtual Result<RenderedComponent> RenderInterpolation(
        IRendererContext context,
        IInterpolationInfo info,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    ) => GetConverterFromOptions(context, info, typingContext, cancellationToken)(
        new RenderedComponent(
            context.GetReferenceToDesignerValue(info, info.Symbol),
            info.Symbol
        )
    );

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
            var quoteCount = (GetSequentialQuoteCount(control.Value) + 1) switch
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

        static int GetSequentialQuoteCount(string text)
        {
            var result = 0;
            var count = 0;
            foreach (var ch in text)
            {
                if (ch is '"')
                {
                    count++;
                    continue;
                }

                if (count > 0)
                {
                    result = Math.Max(result, count);
                    count = 0;
                }
            }

            return Math.Max(result, count);
        }

        static TextControl Join(IReadOnlyList<TextControl> elements)
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

    public abstract Result<RenderedComponent> RenderCheckbox(
        IRendererContext context,
        CheckboxComponentNode checkbox,
        ComponentState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    );

    public abstract Result<RenderedComponent> RenderCheckboxGroupOption(
        IRendererContext context,
        CheckboxGroupOptionComponentNode checkboxGroupOption,
        ComponentState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    );

    public abstract Result<RenderedComponent> RenderCheckboxGroup(
        IRendererContext context,
        CheckboxGroupComponentNode checkboxGroup,
        ComponentState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    );

    public abstract Result<RenderedComponent> RenderRadioGroupOption(
        IRendererContext context,
        RadioGroupOptionComponentNode radioGroupOption,
        ComponentState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    );

    public abstract Result<RenderedComponent> RenderRadioGroup(
        IRendererContext context,
        RadioGroupComponentNode radioGroup,
        ComponentState state,
        RendererTypingContext? typingContext = null,
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