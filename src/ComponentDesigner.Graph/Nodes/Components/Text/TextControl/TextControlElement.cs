using System.Diagnostics.CodeAnalysis;
using System.Text;
using ComponentDesigner.Parser;
using ComponentDesigner.Util;

namespace ComponentDesigner.Nodes.TextControls;

public abstract partial class TextControlElement(
    CXTextSpan textSpan,
    IReadOnlyList<TextControlElement>? children = null)
    : ISourceLocatable
{
    public CXTextSpan TextSpan => textSpan;

    public abstract string Name { get; }

    public virtual IReadOnlyList<TextControlElement>? Children { get; } = children;

    protected TextControlElement(ICXNode node, IReadOnlyList<TextControlElement>? children = null)
        : this(node.TextSpan, children)
    {
    }

    public abstract Result<TextControl> Render(
        IRendererContext context,
        TextControlOptions options,
        CancellationToken cancellationToken = default
    );

    public static bool TryCreate(
        IComponentContext context,
        IEnumerator<ICXNode> enumerator,
        IDiagnosticBag bag,
        out TextControlGraph result,
        out bool enumeratorHasMore,
        CancellationToken cancellationToken = default
    )
    {
        enumeratorHasMore = true;

        var rootElements = new List<TextControlElement>();
        using var _ = ObjectPool<List<CXToken>>.GetScoped(out var tokens);

        do
        {
            if (!TryAddNodes(context, rootElements, enumerator.Current, tokens, bag, cancellationToken, isRoot: true))
                break;
        } while (enumeratorHasMore = enumerator.MoveNext());

        if (rootElements.Count is 0)
        {
            result = default;
            return false;
        }

        CalculateInterpolationDetails(tokens, out bool hasInterpolations, out var interpolationDollarCount);

        result = new(
            rootElements,
            hasInterpolations,
            interpolationDollarCount
        );
        return true;

        static bool TryAddNodes(
            IComponentContext context,
            List<TextControlElement> results,
            ICXNode? cxNode,
            List<CXToken> tokens,
            IDiagnosticBag bag,
            CancellationToken cancellationToken,
            bool isRoot = false
        )
        {
            var i = results.Count;
            AddNodes(context, results, cxNode, tokens, bag, cancellationToken, isRoot);
            return i != results.Count;
        }

        static void AddNodes(
            IComponentContext context,
            List<TextControlElement> results,
            ICXNode? cxNode,
            List<CXToken> tokens,
            IDiagnosticBag bag,
            CancellationToken cancellationToken,
            bool isRoot = false
        )
        {
            switch (cxNode)
            {
                case null: return;

                case CXToken token:
                    results.Add(new SyntaxToken(token));
                    tokens.Add(token);
                    break;

                case CXValue.Scalar scalar:
                    results.Add(new SyntaxToken(scalar.Token));
                    tokens.Add(scalar.Token);
                    break;

                case CXValue.Interpolation interpolation:
                    results.Add(new SyntaxToken(interpolation.Token));
                    tokens.Add(interpolation.Token);
                    break;

                case CXValue.Multipart:
                    // should never occur, api surface expects an enumerator that flattens multipart nodes
                    throw new InvalidOperationException("multipart values are not allowed");

                case CXElement element:
                    if (!context.TextControlProvider.TryGetTextControlFactory(element, out var factory))
                    {
                        if (!isRoot) bag.Add(Diagnostic.UnknownTextControlElement(element).At(element));
                        
                        return;
                    }
                    
                    results.Add(factory(element, CreateChildren(element)));
                    break;

                default:
                    if (!isRoot) bag.Add(Diagnostic.UnsupportedTextControlElement(cxNode).At(cxNode));
                    return;
            }

            IReadOnlyList<TextControlElement> CreateChildren(CXElement element)
            {
                if (element.Children.Count is 0) return [];

                var results = new List<TextControlElement>();

                foreach (var child in GraphNodeEnumerator.GetNext(element.Children))
                {
                    if (!TryAddNodes(context, results, child, tokens, bag, cancellationToken))
                        break;
                }

                return results;
            }
        }

        static void CalculateInterpolationDetails(
            List<CXToken> tokens,
            out bool hasInterpolations,
            out int interpolationDollarCount
        )
        {
            hasInterpolations = false;
            interpolationDollarCount = 0;

            for (var i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];

                switch (token.Kind)
                {
                    case CXTokenKind.Interpolation:
                        hasInterpolations = true;
                        continue;

                    case CXTokenKind.Text:
                        interpolationDollarCount = Math
                            .Max(
                                interpolationDollarCount,
                                GetSequentialInterpolationCharacterCount(token.Value)
                            );
                        continue;
                }
            }

            if (hasInterpolations)
                interpolationDollarCount++;
        }

        static int GetSequentialInterpolationCharacterCount(string part)
        {
            var result = 0;
            var count = 0;
            char? last = null;

            foreach (var ch in part)
            {
                if (ch is '{' or '}')
                {
                    if (last is null)
                    {
                        last = ch;
                        count = 1;
                        continue;
                    }

                    if (last == ch)
                    {
                        count++;
                        continue;
                    }
                }
                
                if (count > 0)
                {
                    result = Math.Max(result, count);
                    last = null;
                    count = 0;
                }
            }
            
            return Math.Max(result, count);
        }
    }

    protected static bool TryGetTextBasedValue(
        CXValue? value,
        IComponentContext context,
        TextControlOptions options,
        [MaybeNullWhen(false)] out string result
    )
    {
        switch (value)
        {
            case CXValue.Scalar scalar:
                result = scalar.Value;
                return true;

            case CXValue.Interpolation interpolation:
                result =
                    $"{options.StartInterpolationMarker}{
                        context.GetReferenceToDesignerValue(interpolation)
                    }{options.StartInterpolationMarker}";
                return true;

            case CXValue.Multipart multipart:
            {
                using var _ = StringBuilder.Pooled(out var sb);

                foreach (var part in multipart.Tokens)
                {
                    switch (part.Kind)
                    {
                        case CXTokenKind.Text:
                            sb.Append(part.Value);
                            break;

                        case CXTokenKind.Interpolation when part.InterpolationIndex is { } index:
                            sb.Append(options.StartInterpolationMarker)
                                .Append(context.GetReferenceToDesignerValue(index))
                                .Append(options.EndInterpolationMarker);
                            break;

                        default:
                            result = null;
                            return false;
                    }
                }

                result = sb.ToString();
                return true;
            }

            default:
                result = null;
                return false;
        }
    }

    protected LexedCXTrivia EnsureLineBreaks(LexedCXTrivia trivia)
    {
        if (trivia.ContainsNewlines) return trivia;

        return [CXTrivia.LineBreak];
    }

    protected string RenderChildrenWithoutNewLines(EquatableArray<TextControl> children)
    {
        using var _ = StringBuilder.Pooled(out var sb);
        sb.Clear();

        var hasTrailingSpace = false;

        for (var i = 0; i < children.Count; i++)
        {
            var child = children[i];

            if (
                i is not 0 &&
                !hasTrailingSpace
            )
            {
                sb.Append(' ');
            }

            sb.Append(child.Value.CollapseAndTrimNewlines());

            hasTrailingSpace =
                i < children.Count - 1 &&
                (
                    child.TrailingTrivia.ContainsWhitespace ||
                    (child.Value.Length is 0 || char.IsWhiteSpace(child.Value[0]))
                );

            if (hasTrailingSpace) sb.Append(' ');
        }

        return sb.ToString();
    }

    protected Result<string> RenderChildrenWithoutNewLines(
        IRendererContext context,
        TextControlOptions options,
        CancellationToken cancellationToken = default
    ) => RenderChildren(context, options, cancellationToken).Map(RenderChildrenWithoutNewLines);

    protected Result<EquatableArray<TextControl>> RenderChildren(
        IRendererContext context,
        TextControlOptions options,
        CancellationToken cancellationToken = default
    )
    {
        if (Children is null or { Count: 0 }) return EquatableArray<TextControl>.Empty;

        var result = new TextControl[Children.Count];
        using var bag = PooledDiagnosticBag.Get();
        var anyFailed = false;

        for (var i = 0; i < Children.Count; i++)
        {
            var childResult = Children[i].Render(context, options, cancellationToken);
            anyFailed |= !childResult.HasValue;
            bag.Add(childResult.Diagnostics);

            if (childResult.HasValue) result[i] = childResult.Value;
        }

        if (anyFailed) return new(bag.ToCollection());

        return new([..result], bag.ToCollection());
    }

    protected static Result<TextControl> JoinWithTrimmedTrivia(
        Result<EquatableArray<TextControl>> target
    ) => Join(target).Map(x => x with
    {
        LeadingTrivia = LexedCXTrivia.Empty,
        TrailingTrivia = LexedCXTrivia.Empty
    });

    protected static Result<TextControl> Join(
        Result<EquatableArray<TextControl>> target
    )
    {
        if (!target.HasValue) return new Result<TextControl>(target.Diagnostics);

        if (target.Value.IsEmpty)
            return new(
                TextControl.Empty,
                target.Diagnostics
            );

        if (target.Value.Count is 1)
            return new(
                target.Value[0],
                target.Diagnostics
            );

        using var _ = StringBuilder.Pooled(out var sb);
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

            hasNewlines |= render.ValueContainsNewLines;
        }

        return new(
            new TextControl(
                target.Value[0].LeadingTrivia,
                target.Value[target.Value.Count - 1].TrailingTrivia,
                sb.ToString(),
                hasNewlines
            )
        );
    }
}