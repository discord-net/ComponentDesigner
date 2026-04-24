using System.Text;
using ComponentDesigner.Nodes;
using ComponentDesigner.Nodes.TextControls;
using ComponentDesigner.Parser;
using ComponentDesigner.Util;

namespace ComponentDesigner.CSharp;

public interface ICSharpRender : ISourceLocatable
{
    string Source { get; init; }
    ICSharpTypeSymbol? Symbol { get; init; }
}

public readonly record struct CSharpRender(
    CXTextSpan TextSpan,
    string Source,
    ICSharpTypeSymbol? Symbol = null
) : ICSharpRender
{
    public bool IsEmpty => TextSpan == default && string.IsNullOrEmpty(Source);
}

public abstract class BaseCSharpRenderer<TGraph> : BaseCSharpRenderer<TGraph, CSharpRender>
{
    protected override CSharpRender CreateFromSource(
        CXTextSpan textSpan,
        string source,
        ICSharpTypeSymbol? symbol
    ) => new(textSpan, source, symbol);
}

public abstract class BaseCSharpRenderer<TGraph, TRender> : IComponentRenderer<TGraph, TRender>
    where TRender : struct, ICSharpRender
{
    protected virtual CSharpValueGenerator? GetCustomGeneratorForSymbol(
        ICompilationProvider compilationProvider,
        ICSharpTypeSymbol symbol
    ) => null;

    private CSharpValueGenerator GetGeneratorForSymbol(
        ICompilationProvider compilationProvider,
        ICSharpTypeSymbol symbol,
        CancellationToken cancellationToken = default
    ) => GetCustomGeneratorForSymbol(compilationProvider, symbol) ??
         CSharpValueGenerator.FromSymbol(compilationProvider, symbol, cancellationToken);

    protected static Result<TRender> Convert(
        IRenderContext<TRender> context,
        TRender render,
        ICSharpTypeSymbol? symbol,
        CancellationToken cancellationToken
    )
    {
        if (context.ComponentTypingProvider is null || render.Symbol is null || symbol is null) return render;

        return context
            .ComponentTypingProvider
            .Convert(
                context,
                render.Source.SourcedAt(render),
                render.Symbol,
                symbol,
                cancellationToken
            )
            .Map(newSource => render with
            {
                Source = newSource,
                Symbol = symbol
            });
    }

    protected abstract TRender CreateFromSource(
        CXTextSpan textSpan,
        string source,
        ICSharpTypeSymbol? symbol
    );

    private Result<TRender> AcceptComponent(
        IRenderContext<TRender> context,
        IComponentNode component,
        ComponentState state,
        CancellationToken cancellationToken = default
    )
    {
        return (component, state) switch
        {
            (InterpolationComponentNode interpolation, InterpolationState interpolationState)
                => RenderInterpolation(context, interpolation, interpolationState, cancellationToken),

            (TextControlNode textControl, TextControlState textControlState)
                => RenderTextControls(context, textControl, textControlState, cancellationToken),

            (FunctionalComponentNode functionalComponent, FunctionalState functionalState)
                => RenderFunctionalComponent(context, functionalComponent, functionalState, cancellationToken),

            _ => RenderComponent(context, component, state, cancellationToken)
        };
    }

    protected virtual Result<TRender> RenderFunctionalComponent(
        IRenderContext<TRender> context,
        FunctionalComponentNode functionalComponent,
        FunctionalState state,
        CancellationToken cancellationToken = default
    )
    {
        var bag = PooledDiagnosticBag.Get();

        using var _ = StringBuilder.Pooled(out var parameters);

        for (var i = 0; i < state.Properties.Count; i++)
        {
            var parameter = state.Properties[i];
            var parameterSymbol = state.Symbol.Parameters[i];

            var parameterValue = state.GetPropertyValue(parameter);

            var result = BuildPropertyValue(parameterSymbol.Type, parameterValue);

            bag.Add(result.Diagnostics);

            if (result.HasValue) AppendParameter(parameters, parameter.Name, result.Value);
        }

        if (bag.HasErrors) return new(bag.ToCollection());

        if (parameters.Length > 0)
        {
            parameters.Insert(0, Environment.NewLine).AppendLine();
        }

        return (
            CreateFromSource(
                state.TextSpan,
                $"{MakeMethodReference(state.CXNode, context, state.Symbol)}({parameters})",
                state.Symbol.ReturnType
            ),
            bag.ToCollection()
        );

        Result<string> BuildPropertyValue(
            ICSharpTypeSymbol typeSymbol,
            ComponentPropertyValue propertyValue
        )
        {
            var isCollection = false;
            var innerSymbol = typeSymbol;

            if (
                !typeSymbol.Equals(context.CompilationProvider.String!) &&
                typeSymbol.TryGetEnumerableType(out var inner)
            )
            {
                isCollection = true;
                innerSymbol = inner;
            }

            using var _ = StringBuilder.Pooled(out var sb);
            using var bag = PooledDiagnosticBag.Get();
            var valueCount = 0;

            foreach (var value in propertyValue.AsFlattened)
            {
                switch (value)
                {
                    case ComponentPropertyValue.Component component:
                        Append(
                            component
                                .GraphNode
                                .Render(
                                    context, cancellationToken
                                )
                                .Unwrap(bag)
                                .Source
                        );
                        break;
                    case ComponentPropertyValue.Literal
                        or ComponentPropertyValue.Interpolation
                        or ComponentPropertyValue.None:
                        Append(
                            GetGeneratorForSymbol(context.CompilationProvider, innerSymbol)
                                .Render(context, value, cancellationToken)
                                .Unwrap(bag)
                                .Source
                        );
                        break;
                }
            }

            if (isCollection)
            {
                if (valueCount is 0)
                {
                    if (propertyValue.Property.IsOptional)
                        return ($"default", bag.ToCollection());

                    return ("[]", bag.ToCollection());
                }

                return (
                    $"""

                     [
                         {sb.ToString().WithNewlinePadding(4)}
                     ]
                     """,
                    bag.ToCollection()
                );
            }

            switch (valueCount)
            {
                case 0:
                    if (propertyValue.Property.IsOptional)
                        return ("default", bag.ToCollection());

                    return Result<string>.FromDiagnostics([
                        Diagnostic
                            .RequiredPropertyNotSpecified(functionalComponent, propertyValue.Property)
                            .At(state.ElementIdentifierTextSpanOrBetter),
                        ..bag.ToCollection()
                    ]);
                case 1:
                    return (sb.ToString(), bag.ToCollection());
                default:
                    return Result<string>.FromDiagnostics([
                        Diagnostic
                            .TooManyPropertyValues(propertyValue.Property)
                            .At(propertyValue),
                        ..bag.ToCollection()
                    ]);
            }

            void Append(string? value)
            {
                if (string.IsNullOrWhiteSpace(value))
                    return;

                valueCount++;

                if (sb.Length > 0)
                    sb.AppendLine(",");

                sb.Append(value);
            }
        }


        static void AppendParameter(StringBuilder builder, string name, string value)
        {
            if (builder.Length > 0) builder.AppendLine(",");
            builder.Append("    ").Append(name).Append(": ").Append(value.WithNewlinePadding(4));
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

    protected virtual Result<TRender> RenderTextControls(
        IRenderContext<TRender> context,
        TextControlNode textControl,
        TextControlState state,
        CancellationToken cancellationToken = default
    )
    {
        var options = new TextControlOptions(CreateInterpolationRenderer(state.TextControlGraph));

        var renders = state
            .TextControlGraph
            .RootElements
            .Select(element => element.Render(context, options));

        return Collect(renders)
            .Map(control => ToCSharpString(control, state.TextControlGraph))
            .Map(str => CreateFromSource(
                state.TextSpan,
                str,
                context.CompilationProvider.String(CXTextSpan.Empty).GetValueOrDefault())
            );

        static string ToCSharpString(
            TextControl control,
            TextControlGraph graph
        )
        {
            var stringQuoteCount = GetSequentialQuoteCount(control.Value) switch
            {
                // we need 3 quotes to escape
                1 or 2 => 3,

                // always have one more quote than the value contains sequentially
                var other => other + 1
            };

            var asMultilineString = control.ContainsNewLines || stringQuoteCount > 1;
            var asMultilineInterpolation = asMultilineString && graph.ContainsInterpolations;

            if (asMultilineString)
            {
                // multiline strings must have at least 3 quotes
                stringQuoteCount = Math.Max(3, stringQuoteCount);
            }

            using var _ = StringBuilder.Pooled(out var sb);

            if (asMultilineString)
            {
                // multiline strings start on a new line
                sb.AppendLine();
            }

            if (graph.ContainsInterpolations)
            {
                sb.Append('$', graph.InterpolationDollarSignRequirement);
            }

            sb.Append('"', stringQuoteCount);

            if (asMultilineString)
                sb.AppendLine();

            var value = control.ToString().NormalizeIndentation();

            if (asMultilineInterpolation)
                value = value.Indent(graph.InterpolationDollarSignRequirement);

            sb.Append(value);

            if (asMultilineString)
                sb.AppendLine();

            if (asMultilineString)
                sb.Append(' ', graph.InterpolationDollarSignRequirement);

            sb.Append('"', stringQuoteCount);

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


        static Result<TextControl> Collect(
            IEnumerable<Result<TextControl>> controls
        )
        {
            using var result = Result<TextControl>.Builder;
            using var _ = StringBuilder.Pooled(out var sb);
            var containsNewLines = false;

            TextControl? first = null;
            TextControl? last = null;

            foreach (var controlResult in controls)
            {
                if (last is not null)
                {
                    sb.Append(last.Value.TrailingTrivia);
                    containsNewLines |= last.Value.TrailingTrivia.ContainsNewlines;
                }

                result.AddDiagnostics(controlResult.Diagnostics);

                if (!controlResult.HasValue)
                    continue;

                var control = controlResult.Value;

                first ??= control;

                if (sb.Length is not 0)
                {
                    sb.Append(control.LeadingTrivia);
                    containsNewLines |= control.LeadingTrivia.ContainsNewlines;
                }

                sb.Append(control.Value);
                containsNewLines |= control.ValueContainsNewLines;

                last = control;
            }

            return result
                .WithValue(
                    new TextControl(
                        LeadingTrivia: first?.LeadingTrivia ?? LexedCXTrivia.Empty,
                        TrailingTrivia: last?.TrailingTrivia.TrimTrailingSyntaxIndentation() ?? LexedCXTrivia.Empty,
                        Value: sb.ToString(),
                        ValueContainsNewLines: containsNewLines
                    )
                )
                .Build();
        }

        static TextControlInterpolationRenderer CreateInterpolationRenderer(TextControlGraph graph)
        {
            if (!graph.ContainsInterpolations)
                return Empty;

            var startInterpolation = graph.ContainsInterpolations
                ? new string('{', graph.InterpolationDollarSignRequirement)
                : string.Empty;

            var endInterpolation = graph.ContainsInterpolations
                ? new string('}', graph.InterpolationDollarSignRequirement)
                : string.Empty;

            return (context, info, out valueContainsNewLines) =>
            {
                valueContainsNewLines = false;
                return $"{startInterpolation}{context.GetReferenceToDesignerValue(info)}{endInterpolation}";
            };

            static Result<string> Empty(
                IRenderContext context,
                IInterpolationInfo info,
                out bool valueContainsNewlines
            )
            {
                valueContainsNewlines = false;
                return string.Empty;
            }
        }

        // Result<string> RenderInterpolation(
        //     IRenderContext context,
        //     IInterpolationInfo info,
        //     out bool valueContainsNewLines
        // )
        // {
        //     
        // }

        // var startInterpolation = state.TextControlGraph.ContainsInterpolations
        //     ? new string('{', state.TextControlGraph.InterpolationDollarSignRequirement)
        //     : string.Empty;
        //
        // var endInterpolation = state.TextControlGraph.ContainsInterpolations
        //     ? new string('}', state.TextControlGraph.InterpolationDollarSignRequirement)
        //     : string.Empty;
        //
        // var options = new TextControlOptions(
        //     startInterpolation,
        //     endInterpolation
        // );
    }

    protected virtual Result<TRender> RenderInterpolation(
        IRenderContext<TRender> context,
        InterpolationComponentNode interpolation,
        InterpolationState state,
        CancellationToken cancellationToken = default
    ) => CreateFromSource(
        state.TextSpan,
        context.GetReferenceToDesignerValue(state.InterpolationId, state.Symbol),
        state.Symbol
    );

    public abstract Result<TRender> RenderComponent(
        IRenderContext<TRender> context,
        IComponentNode component,
        ComponentState state,
        CancellationToken cancellationToken = default
    );

    public abstract Result<TGraph> RenderGraph(
        IRenderContext<TRender> context,
        CXComponentGraph graph,
        CancellationToken cancellationToken = default
    );

    Result<TRender> IComponentRenderer<TRender>.RenderComponent(
        IRenderContext<TRender> context,
        IComponentNode component,
        ComponentState state,
        CancellationToken cancellationToken
    ) => AcceptComponent(context, component, state, cancellationToken);
}