using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Discord.CX.Nodes.Text.Controls;
using Discord.CX.Parser;

namespace Discord.CX.Nodes.Text;

public readonly record struct TextControl(
    LexedCXTrivia LeadingTrivia,
    LexedCXTrivia TrailingTrivia,
    string Value,
    bool ValueContainsNewLines
)
{
    public bool ContainsNewLines
        => ValueContainsNewLines || LeadingTrivia.ContainsNewlines || TrailingTrivia.ContainsNewlines;

    public static readonly TextControl Empty = new(LexedCXTrivia.Empty, LexedCXTrivia.Empty, string.Empty, false);

    public override string ToString()
        => $"{LeadingTrivia}{Value}{TrailingTrivia}";
}

public readonly record struct TextControlOptions(
    string StartInterpolationMarker,
    string EndInterpolationMarker,
    bool AsCSharpString
)
{
    public static readonly TextControlOptions Default = new(string.Empty, string.Empty, false);
}

public abstract class TextControlElement(CXTextSpan textSpan, IReadOnlyList<TextControlElement>? children = null)
{
    public CXTextSpan TextSpan { get; } = textSpan;

    public abstract string Name { get; }

    public virtual IReadOnlyList<Type>? AllowedChildren => null;

    public IReadOnlyList<TextControlElement>? Children { get; } = children;

    public TextControlElement(ICXNode node, IReadOnlyList<TextControlElement>? children = null)
        : this(node.Span, children)
    {
    }

    public static bool TryCreate(
        IGraphContext context,
        IEnumerator<ICXNode> nodes,
        IList<Diagnostic> bag,
        [MaybeNullWhen(false)] out TextControlElement result,
        out bool hasMore,
        CancellationToken cancellationToken = default
    )
    {
        hasMore = true;
        
        var elements = new List<TextControlElement>();
        var tokens = ObjectPool<List<CXToken>>.Get();
        tokens.Clear();
        elements.Clear();

        do
        {
            if (!TryCreateNode(elements, context, nodes.Current, tokens, bag, cancellationToken))
                break;
        } while (hasMore = nodes.MoveNext());

        if (elements.Count is 0)
        {
            result = null;
            return false;
        }

        result = RootTextControlElement.Create(
            tokens,
            elements
        );

        return true;

        static bool TryCreateNode(
            List<TextControlElement> results,
            IGraphContext context,
            ICXNode? node,
            List<CXToken> tokens,
            IList<Diagnostic> bag,
            CancellationToken cancellationToken,
            bool isRoot = false
        )
        {
            var i = results.Count;
            Create(results, context, node, tokens, bag, cancellationToken, isRoot);
            return i != results.Count;
        }

        static void Create(
            List<TextControlElement> results,
            IGraphContext context,
            ICXNode? node,
            List<CXToken> tokens,
            IList<Diagnostic> bag,
            CancellationToken cancellationToken,
            bool isRoot = false
        )
        {
            if (node is null) return;
            
            switch (node)
            {
                case CXToken token:
                    results.Add(new ScalarTextControlElement(token));
                    tokens.Add(token);
                    return;

                case CXValue.Scalar scalar:
                    results.Add(new ScalarTextControlElement(scalar.Token));
                    tokens.Add(scalar.Token);
                    return;

                case CXValue.Interpolation interpolation:
                    results.Add(new ScalarTextControlElement(interpolation.Token));
                    tokens.Add(interpolation.Token);
                    return;

                case CXValue.Multipart multipart:
                    // we shouldn't ever see a multi-part
                    throw new InvalidOperationException("multi-parts not allowed in text-control");

                case CXElement element:
                    var control = element.Identifier.ToLowerInvariant() switch
                    {
                        "b" or "strong" or "bold" => new BoldTextControlElement(element, CreateChildren(element)),
                        _ => null
                    };

                    if (control is null)
                    {
                        if (!isRoot)
                        {
                            bag.Add(
                                element.Report(
                                    Diagnostic.UnknownTextControlElement(element)
                                )
                            );
                        }

                        return;
                    }
                    
                    results.Add(control);
                    return;
                default:
                    if (!isRoot)
                    {
                        bag.Add(
                            node.Report(
                                Diagnostic.UnsupportedTextControlElement(node)
                            )
                        );
                    }

                    return;
            }

            IReadOnlyList<TextControlElement> CreateChildren(CXElement element)
            {
                if (element.Children.Count is 0) return [];

                var children = new List<TextControlElement>();

                foreach (var child in element.Children)
                {
                    if (!TryCreateNode(children, context, child, tokens, bag, cancellationToken))
                        break;
                }

                return children;
            }
        }
    }

    public Result<string> RenderToCSharpString(IRendererContext context, CancellationToken token = default)
        => Render(context, TextControlOptions.Default with { AsCSharpString = true }, token)
            .Map(x => x.ToString());

    protected abstract Result<TextControl> Render(
        IRendererContext context,
        TextControlOptions options,
        CancellationToken token = default
    );

    protected Result<EquatableArray<TextControl>> RenderChildren(
        IRendererContext context,
        TextControlOptions options,
        CancellationToken token = default
    )
    {
        if (Children is null or { Count: 0 }) return EquatableArray<TextControl>.Empty;

        var result = new TextControl[Children.Count];
        var bag = DiagnosticBag.Get();
        var anyFailed = false;


        for (var i = 0; i < Children.Count; i++)
        {
            var childResult = Children[i].Render(context, options, token);
            anyFailed |= !childResult.HasValue;
            bag.AddDiagnostics(childResult.Diagnostics);

            if (childResult.HasValue) result[i] = childResult.Value;
        }

        if (anyFailed) return new(bag.Use());

        return new([..result], bag.Use());
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