using System.Diagnostics.CodeAnalysis;
using System.Text;
using ComponentDesigner.Parser;

namespace ComponentDesigner.Nodes.TextControls;

public abstract class TextControlElement(CXTextSpan textSpan, IReadOnlyList<TextControlElement>? children = null)
{
    public CXTextSpan TextSpan => textSpan;

    public abstract string Name { get; }

    public virtual IReadOnlyList<TextControlElement>? Children { get; } = children;

    public virtual IReadOnlyList<Type>? AllowedChildren => null;

    protected TextControlElement(ICXNode node, IReadOnlyList<TextControlElement>? children = null) 
        : this(node.Span, children)
    {
    }

    public abstract Result<TextControl> Render(
        IRendererContext context,
        TextControlOptions options,
        CancellationToken cancellationToken = default
    );

    public static bool TryCreate(
        IGraphContext context,
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
            IGraphContext context,
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
            IGraphContext context,
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
                    results.Add(new ScalarTextControlElement(token));
                    tokens.Add(token);
                    break;

                case CXValue.Scalar scalar:
                    results.Add(new ScalarTextControlElement(scalar.Token));
                    tokens.Add(scalar.Token);
                    break;

                case CXValue.Interpolation interpolation:
                    results.Add(new ScalarTextControlElement(interpolation.Token));
                    tokens.Add(interpolation.Token);
                    break;

                case CXValue.Multipart:
                    // should never occur, api surface expects an enumerator that flattens multipart nodes
                    throw new InvalidOperationException("multipart values are not allowed");

                case CXElement element:
                    var control = element.Identifier.ToLowerInvariant() switch
                    {
                        "b" or "strong" or "bold" => new BoldTextControlElement(element, CreateChildren(element)),
                        _ => null
                    };

                    if (control is null)
                    {
                        if (!isRoot) bag.Add(Diagnostic.UnknownTextControlElement(element).At(element));

                        return;
                    }

                    results.Add(control);
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
                                StringGenerator.GetInterpolationDollarRequirement(token.Value)
                            );
                        continue;
                }
            }
        }
    }
    
    protected Result<EquatableArray<TextControl>> RenderChildren(
        IRendererContext context,
        TextControlOptions options,
        CancellationToken token = default
    )
    {
        if (Children is null or { Count: 0 }) return EquatableArray<TextControl>.Empty;

        var result = new TextControl[Children.Count];
        using var bag = PooledDiagnosticBag.Get();
        var anyFailed = false;
        
        for (var i = 0; i < Children.Count; i++)
        {
            var childResult = Children[i].Render(context, options, token);
            anyFailed |= !childResult.HasValue;
            bag.Add(childResult.Diagnostics);

            if (childResult.HasValue) result[i] = childResult.Value;
        }

        if (anyFailed) return new(bag.ToCollection());

        return new([..result], bag.ToCollection());
    }

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

        using var _ = ObjectPool<StringBuilder>.GetScoped(out var sb);
        sb.Clear();
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