using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Discord.CX.Nodes.Components.Controls;
using Discord.CX.Parser;
using Microsoft.CodeAnalysis.Text;

namespace Discord.CX.Nodes.Components;

public readonly record struct RenderedTextControlElement(
    LexedCXTrivia LeadingTrivia,
    LexedCXTrivia TrailingTrivia,
    bool ValueHasNewLines,
    string Value
)
{
    public bool HasNewlines => ValueHasNewLines || LeadingTrivia.ContainsNewlines || TrailingTrivia.ContainsNewlines;

    public static readonly RenderedTextControlElement Empty = new(
        LexedCXTrivia.Empty,
        LexedCXTrivia.Empty,
        false,
        string.Empty
    );

    public override string ToString()
        => $"{LeadingTrivia}{Value}{TrailingTrivia}";
}

public abstract class TextControlElement(TextSpan span)
{
    public TextSpan Span => span;
    public abstract string FriendlyName { get; }

    public virtual IReadOnlyList<TextControlElement> Children => [];

    public virtual IReadOnlyList<Type>? AllowedChildren => null;

    public TextControlElement(ICXNode node) : this(node.Span)
    {
    }

    public static bool TryCreate(
        IComponentContext context,
        IReadOnlyList<CXNode> nodes,
        IList<DiagnosticInfo> diagnostics,
        [MaybeNullWhen(false)] out TextControlElement textControlElement,
        out int nodesUsed,
        int? startingIndex = null
    )
    {
        if (nodes.Count is 0 || startingIndex >= nodes.Count)
        {
            nodesUsed = 0;
            textControlElement = null;
            return false;
        }

        var elements = new List<TextControlElement>();
        var tokens = new List<CXToken>();

        var index = startingIndex ?? 0;
        var nodeStartingIndex = index;
        for (; index < nodes.Count; index++)
        {
            if (Create(context, nodes[index], tokens, diagnostics, isRoot: true) is not { } element)
                break;

            elements.AddRange(element);
        }

        nodesUsed = index - nodeStartingIndex;

        if (elements.Count is 0)
        {
            textControlElement = null;
            return false;
        }

        textControlElement = new Root(tokens, elements);
        return true;

        static IEnumerable<TextControlElement>? Create(
            IComponentContext context,
            CXNode node,
            List<CXToken> tokens,
            IList<DiagnosticInfo> diagnostics,
            bool isRoot = false
        )
        {
            switch (node)
            {
                case CXValue.Multipart multipart:
                    if (!isRoot)
                    {
                        tokens.AddRange(multipart.Tokens);
                        return multipart.Tokens.Select(x => new Scalar(x));
                    }

                    List<TextControlElement>? result = null;

                    foreach (var token in multipart.Tokens)
                    {
                        if (
                            token.InterpolationIndex is { } index &&
                            ComponentBuilderKind.IsValidComponentBuilderType(
                                context.GetInterpolationInfo(index).Symbol,
                                context.Compilation
                            )
                        ) break;

                        tokens.AddRange(multipart.Tokens);
                        (result ??= []).Add(new Scalar(token));
                    }

                    return result;
                case CXValue.Interpolation interpolation:
                    if (
                        isRoot &&
                        ComponentBuilderKind.IsValidComponentBuilderType(
                            context.GetInterpolationInfo(interpolation).Symbol,
                            context.Compilation
                        )
                    ) return null;

                    tokens.Add(interpolation.Token);
                    return [new Scalar(interpolation.Token)];
                case CXValue.Scalar scalar:
                    tokens.Add(scalar.Token);
                    return [new Scalar(scalar.Token)];
                case CXElement element:
                    switch (element.Identifier.ToLowerInvariant())
                    {
                        case "b" or "bold" or "strong":
                            return [new BoldTextControlElement(element, CreateChildren(element))];
                        case "i" or "italic" or "italics" or "em":
                            return [new ItalicTextControlElement(element, CreateChildren(element))];

                        case "u" or "mark" or "underline" or "ins":
                            return [new UnderlineTextControlElement(element, CreateChildren(element))];

                        case "del" or "strike" or "strikethrough":
                            return [new StrikethroughTextControlElement(element, CreateChildren(element))];
                        case "sub" or "subtext" or "small":
                            return [new SubtextTextControlElement(element, CreateChildren(element))];
                        case "link" or "a":
                            return [new LinkTextControlElement(element, CreateChildren(element))];
                        case "ul" or "list":
                            return
                            [
                                new ListTextControlElement(element, ListTextControlElementKind.Unordered,
                                    CreateChildren(element))
                            ];
                        case "ol":
                            return
                            [
                                new ListTextControlElement(element, ListTextControlElementKind.Ordered,
                                    CreateChildren(element))
                            ];
                        case "li":
                            return [new ListItemTextControlElement(element, CreateChildren(element))];

                        case "c" or "code" or "block" or "codeblock":
                            return [new CodeTextControlElement(element, CreateChildren(element))];

                        case "blockquote" or "quote" or "q":
                            return [new QuoteTextControlElement(element, CreateChildren(element))];

                        case "spoiler" or "obfuscated":
                            return [new SpoilerTextControlElement(element, CreateChildren(element))];

                        case "br" or "break":
                            return [new LineBreakTextControlElement(element, CreateChildren(element))];

                        case "time" or "timestamp" when TimeTagTextControlElement.TryCreate(
                            context,
                            element,
                            diagnostics,
                            tokens,
                            out var tag
                        ): return [tag];

                        case var _ when MentionTextControlElement.TryCreate(
                            context, element, diagnostics,  tokens, out var mention
                        ): return [mention];

                        case var identifier when Enum.TryParse<HeadingTextControlElementVariant>(
                            identifier,
                            ignoreCase: true,
                            out var headerVariant
                        ): return [new HeadingTextControlElement(element, headerVariant, CreateChildren(element))];

                        default:
                            if (!isRoot)
                            {
                                diagnostics.Add(
                                    Diagnostics.UnknownComponent(element.Identifier),
                                    element
                                );
                            }

                            return null;
                    }
                default:
                    if (!isRoot)
                    {
                        diagnostics.Add(
                            Diagnostics.UnknownComponent(node.GetType().Name),
                            node
                        );
                    }

                    return null;
            }

            IReadOnlyList<TextControlElement> CreateChildren(CXElement element)
            {
                var children = new List<TextControlElement>();

                foreach (var child in element.Children)
                {
                    if (Create(context, child, tokens, diagnostics) is not { } childTextControl)
                        break;

                    children.AddRange(childTextControl);
                }

                return children;
            }
        }
    }

    protected static void AddTokensFromValue(List<CXToken> tokens, CXValue? value)
    {
        if (value is null) return;

        switch (value)
        {
            case CXValue.Scalar scalar: tokens.Add(scalar.Token); break;
            case CXValue.Interpolation interpolation: tokens.Add(interpolation.Token); break;
            case CXValue.Multipart multipart: tokens.AddRange(multipart.Tokens); break;
        }
    }

    public Result<string> RenderToCSharpString(IComponentContext context)
        => Render(context, TextControlRenderingOptions.Default with
            {
                AsCSharpString = true
            })
            .Map(x => x.ToString());

    public Result<string> Render(IComponentContext context)
        => Render(context, TextControlRenderingOptions.Default)
            .Map(x => x.ToString());

    public virtual void Validate(IComponentContext context, IList<DiagnosticInfo> diagnostics)
    {
        if (AllowedChildren is not null)
        {
            foreach (var child in Children)
            {
                if (child is Scalar) continue;

                if (!AllowedChildren.Contains(child.GetType()))
                {
                    diagnostics.Add(
                        Diagnostics.InvalidChild(FriendlyName, child.FriendlyName),
                        span
                    );
                }
            }
        }

        foreach (var child in Children)
        {
            child.Validate(context, diagnostics);
        }
    }

    protected abstract Result<RenderedTextControlElement> Render(
        IComponentContext context,
        TextControlRenderingOptions options
    );

    protected Result<EquatableArray<RenderedTextControlElement>> RenderChildren(
        IComponentContext context,
        TextControlRenderingOptions options
    ) => Children.Select(x => x.Render(context, options)).FlattenAll();

    protected static Result<RenderedTextControlElement> Join(
        Result<EquatableArray<RenderedTextControlElement>> target
    )
    {
        if (!target.HasResult) return target.Diagnostics;

        if (target.Value.IsEmpty)
            return new(
                RenderedTextControlElement.Empty,
                target.Diagnostics
            );

        if (target.Value.Count is 1)
            return new(
                target.Value[0],
                target.Diagnostics
            );

        var sb = new StringBuilder();
        var hasNewlines = false;

        for (var i = 0; i < target.Value.Count; i++)
        {
            var render = target.Value[i];

            if (i is not 0)
            {
                sb.Append(render.LeadingTrivia);
                hasNewlines |= render.LeadingTrivia.ContainsNewlines;
            }

            sb.Append(render.Value);

            if (i < target.Value.Count - 1)
            {
                sb.Append(render.TrailingTrivia);
                hasNewlines |= render.TrailingTrivia.ContainsNewlines;
            }

            hasNewlines |= render.ValueHasNewLines;
        }

        return new(
            new RenderedTextControlElement(
                target.Value[0].LeadingTrivia,
                target.Value[target.Value.Count - 1].TrailingTrivia,
                hasNewlines,
                sb.ToString()
            )
        );
    }

    protected static Result<RenderedTextControlElement> JoinWithTrimmedTrivia(
        Result<EquatableArray<RenderedTextControlElement>> target
    ) => Join(target).Map(x => x with
    {
        LeadingTrivia = LexedCXTrivia.Empty,
        TrailingTrivia = LexedCXTrivia.Empty
    });

    protected LexedCXTrivia EnsureLineBreaks(LexedCXTrivia trivia)
    {
        if (trivia.ContainsNewlines) return trivia;
        return [new CXTrivia.Token(CXTriviaTokenKind.Newline, Environment.NewLine)];
    }

    protected static Result<string> ToTextBasedValue(
        CXValue value,
        IComponentContext context,
        TextControlRenderingOptions options
    )
    {
        switch (value)
        {
            case CXValue.Scalar scalar:
                return scalar.Value;
            case CXValue.Interpolation interpolation:
                return
                    $"{options.StartInterpolation}{context.GetDesignerValue(interpolation)}{options.EndInterpolation}";
            case CXValue.Multipart multipart:
                var sb = new StringBuilder();

                foreach (var part in multipart.Tokens)
                {
                    switch (part.Kind)
                    {
                        case CXTokenKind.Text:
                            sb.Append(part.Value);
                            break;
                        case CXTokenKind.Interpolation when part.InterpolationIndex is { } index:
                            sb.Append(options.StartInterpolation)
                                .Append(context.GetDesignerValue(index))
                                .Append(options.EndInterpolation);
                            break;
                        default:
                            return new DiagnosticInfo(
                                Diagnostics.InvalidPropertyValueSyntax("text or interpolation"),
                                part
                            );
                    }
                }

                return sb.ToString();
            default:
                return new DiagnosticInfo(
                    Diagnostics.InvalidPropertyValueSyntax("text or interpolation"),
                    value
                );
        }
    }

    protected string RenderChildrenWithoutNewlines(EquatableArray<RenderedTextControlElement> children)
    {
        var sb = new StringBuilder();

        // value trivia is collapsed to whitespace only
        var hasTrailingSpace = false;
        for (var i = 0; i < children.Count; i++)
        {
            var child = children[i];

            if (
                i is not 0 &&
                !hasTrailingSpace &&
                (
                    child.LeadingTrivia.Any(x => x.IsWhitespaceTrivia) ||
                    (child.Value.Length is 0 || char.IsWhiteSpace(child.Value[0]))
                )
            ) sb.Append(' ');

            sb.Append(child.Value.CollapseAndTrimNewlines());

            hasTrailingSpace
                = i < children.Count - 1 &&
                  (
                      child
                          .TrailingTrivia
                          .Any(x => x.IsWhitespaceTrivia) ||
                      (child.Value.Length is 0 || char.IsWhiteSpace(child.Value[child.Value.Length - 1]))
                  );

            if (hasTrailingSpace) sb.Append(' ');
        }

        return sb.ToString();
    }

    private sealed class Root(
        IReadOnlyList<CXToken> tokens,
        IReadOnlyList<TextControlElement> children
    ) : TextControlElement(
        tokens.Count is 0
            ? default
            : TextSpan.FromBounds(tokens[0].Span.Start, tokens[tokens.Count - 1].Span.End)
    )
    {
        public override string FriendlyName => "Root";

        public override IReadOnlyList<TextControlElement> Children => children;

        protected override Result<RenderedTextControlElement> Render(
            IComponentContext context,
            TextControlRenderingOptions options
        )
        {
            var literalParts = new List<CXToken>();
            var hasInterpolations = false;

            foreach (var token in tokens)
            {
                if (token.Kind is CXTokenKind.Text)
                    literalParts.Add(token);
                else hasInterpolations = true;
            }

            var interpolationInjectionCount = literalParts.Count is 0
                ? 1
                : Math.Max(
                    1,
                    literalParts.Select(x => StringGenerator.GetInterpolationDollarRequirement(x.Value)).Max()
                );


            var startInterpolation = hasInterpolations
                ? new string('{', interpolationInjectionCount)
                : string.Empty;

            var endInterpolation = hasInterpolations
                ? new string('}', interpolationInjectionCount)
                : string.Empty;

            options = new TextControlRenderingOptions(
                startInterpolation,
                endInterpolation,
                options.AsCSharpString
            );

            var result = Join(RenderChildren(context, options)).Map(x => x with
            {
                LeadingTrivia = x.LeadingTrivia.TrimLeadingSyntaxIndentation(),
                TrailingTrivia = x.TrailingTrivia.TrimTrailingSyntaxIndentation()
            });

            if (options.AsCSharpString)
            {
                result = result
                    .Map(x =>
                    {
                        var quoteCount = (StringGenerator.GetSequentialQuoteCount(x.Value) + 1) switch
                        {
                            2 => 3,
                            var r => r
                        };

                        var isMultiline = x.HasNewlines;
                        var isMultilineInterpolation = isMultiline && hasInterpolations;

                        if (isMultiline)
                            quoteCount = Math.Max(3, quoteCount);

                        var dollars = hasInterpolations
                            ? new string(
                                '$',
                                interpolationInjectionCount
                            )
                            : string.Empty;

                        var quotes = new string('"', quoteCount);

                        var pad = isMultilineInterpolation
                            ? new string(' ', interpolationInjectionCount)
                            : string.Empty;

                        var sb = new StringBuilder();

                        // start on newline if its a multi-line string
                        if (isMultiline) sb.AppendLine();

                        sb.Append(dollars).Append(quotes);

                        if (isMultiline) sb.AppendLine();

                        var value = x.ToString().NormalizeIndentation().Trim(['\r', '\n']);

                        if (isMultilineInterpolation)
                            value = value.Indent(interpolationInjectionCount);

                        sb.Append(value);

                        if (isMultiline) sb.AppendLine();

                        if (isMultilineInterpolation) sb.Append(pad);
                        sb.Append(quotes);

                        return new RenderedTextControlElement(
                            LexedCXTrivia.Empty,
                            LexedCXTrivia.Empty,
                            isMultiline,
                            sb.ToString()
                        );
                    });
            }

            return result;
        }
    }

    private sealed class Scalar(CXToken token) : TextControlElement(token)
    {
        public override string FriendlyName => token.Kind.ToString();

        protected override Result<RenderedTextControlElement> Render(
            IComponentContext context,
            TextControlRenderingOptions options
        ) => new RenderedTextControlElement(
            token.LeadingTrivia,
            token.TrailingTrivia,
            Value: RenderToken(context, options, out var isMultiLine),
            ValueHasNewLines: isMultiLine
        );

        private string RenderToken(
            IComponentContext context,
            TextControlRenderingOptions options,
            out bool hasMultiLine
        )
        {
            if (token.InterpolationIndex is { } index)
            {
                var info = context.GetInterpolationInfo(index);

                if (info.Constant.HasValue)
                {
                    var val = info.Constant.Value?.ToString() ?? string.Empty;
                    hasMultiLine = val.Contains('\n');
                    return val;
                }

                hasMultiLine = false;
                return $"{options.StartInterpolation}{context.GetDesignerValue(index)}{options.EndInterpolation}";
            }

            hasMultiLine = token.Value.Contains('\n');
            return token.Value;
        }
    }
}