using System.Text;
using ComponentDesigner.Nodes;
using ComponentDesigner.Parser;
using ComponentDesigner.Parser.Util;
using ComponentDesigner.Util;

namespace ComponentDesigner;

public enum StringNullMode
{
    DisallowNull,
    AllowNull,
    TreatNullAsEmptyString
}

public sealed class StringGenerator : CSharpValueGenerator
{
    public bool AllowsNull => _stringMode == StringNullMode.AllowNull;
    public bool DisallowsNull => _stringMode == StringNullMode.DisallowNull;
    public bool TreatsNullAsEmptyString => _stringMode == StringNullMode.TreatNullAsEmptyString;

    private readonly StringNullMode _stringMode;

    private StringGenerator(StringNullMode stringMode)
    {
        _stringMode = stringMode;
    }

    public static StringGenerator Get(StringNullMode stringMode)
        => WeakMemoize.Of(stringMode, static a => new StringGenerator(a));

    public override Result<string> Render(
        IRendererContext context,
        ComponentPropertyValue value,
        CancellationToken cancellationToken = default
    )
    {
        if (
            value is ComponentPropertyValue.Many
            or ComponentPropertyValue.Literal
            or ComponentPropertyValue.Interpolation
        )
        {
            return ToCSharpString(context, value);
        }

        return base.Render(context, value, cancellationToken);
    }

    private readonly ref struct PartsBuilder : IDisposable
    {
        public bool HasInterpolations => Interpolations.Count > 0;
        public int Count => Sequence.Count;

        public List<ContainsTrivia<string>> Literals { get; }
        public List<ContainsTrivia<IInterpolationInfo>> Interpolations { get; }
        public List<bool> Sequence { get; }

        public PartsBuilder()
        {
            Literals = ObjectPool<List<ContainsTrivia<string>>>.Get();
            Interpolations = ObjectPool<List<ContainsTrivia<IInterpolationInfo>>>.Get();
            Sequence = ObjectPool<List<bool>>.Get();

            Literals.Clear();
            Interpolations.Clear();
            Sequence.Clear();
        }

        public bool IsInterpolationAt(int index)
            => Sequence[index];

        public void Add(ContainsTrivia<string> part)
        {
            Literals.Add(part);
            Sequence.Add(false);
        }

        public void Add(ContainsTrivia<IInterpolationInfo> part)
        {
            Interpolations.Add(part);
            Sequence.Add(true);
        }

        public void Dispose()
        {
            ObjectPool<List<ContainsTrivia<string>>>.Return(Literals);
            ObjectPool<List<ContainsTrivia<IInterpolationInfo>>>.Return(Interpolations);
            ObjectPool<List<bool>>.Return(Sequence);
        }
    }

    public static Result<string> ToCSharpString(IComponentContext context, string text)
    {
        return ToCSharpString(
            context,
            text,
            ExtractParts
        );

        static void ExtractParts(
            IComponentContext context,
            string value,
            ref readonly PartsBuilder parts,
            IDiagnosticBag bag
        )
        {
            parts.Add(value.WithNoTrivia);
        }
    }

    public static Result<string> ToCSharpString(IComponentContext context, ComponentPropertyValue value)
    {
        return ToCSharpString(context, value, ExtractParts);

        static void ExtractParts(
            IComponentContext context,
            ComponentPropertyValue value,
            ref readonly PartsBuilder parts,
            IDiagnosticBag bag
        )
        {
            switch (value)
            {
                case ComponentPropertyValue.Literal { Value: var literal }:
                    parts.Add(literal.WithTriviaFrom(value));
                    return;

                case ComponentPropertyValue.Interpolation { Info: var info }:
                    parts.Add(info.WithTriviaFrom(value));
                    return;

                case ComponentPropertyValue.Many { Values: var values }:
                    foreach (var subValue in values)
                        ExtractParts(context, subValue, in parts, bag);

                    return;

                default:
                    bag.Add(
                        Diagnostic
                            .InvalidPropertyValue(
                                value,
                                ComponentPropertyValueKind.SyntaxValue
                            )
                            .At(value)
                    );
                    return;
            }
        }
    }

    public static Result<string> ToCSharpString(IComponentContext context, CXValue value)
    {
        return ToCSharpString(context, value, ExtractParts);

        static void ExtractParts(
            IComponentContext context,
            CXValue value,
            ref readonly PartsBuilder parts,
            IDiagnosticBag bag
        )
        {
            switch (value)
            {
                case CXValue.Interpolation interpolation:
                    parts.Add(
                        context
                            .GetInterpolationInfo(interpolation)
                            .WithTriviaFrom(interpolation)
                    );
                    break;
                case CXValue.Multipart multipart:
                    foreach (var token in multipart.Tokens)
                    {
                        switch (token.Kind)
                        {
                            case CXTokenKind.Text:
                                parts.Add(
                                    token
                                        .Value
                                        .WithTriviaFrom(token)
                                );
                                continue;

                            case CXTokenKind.Interpolation when token.InterpolationIndex is { } index:
                                parts.Add(
                                    context
                                        .GetInterpolationInfo(index)
                                        .WithTriviaFrom(token)
                                );
                                continue;
                            default:
                                bag.Add(
                                    Diagnostic
                                        .InvalidSyntaxValue(token)
                                        .At(token)
                                );
                                continue;
                        }
                    }

                    break;
                case CXValue.Scalar scalar:
                    parts.Add(
                        scalar
                            .Value
                            .WithTriviaFrom(scalar)
                    );
                    break;
                default:
                    bag.Add(
                        Diagnostic
                            .InvalidSyntaxValue(value)
                            .At(value)
                    );
                    return;
            }
        }
    }

    private delegate void PartExtractor<in TValue>(
        IComponentContext context,
        TValue value,
        scoped ref readonly PartsBuilder parts,
        IDiagnosticBag bag
    );

    private static Result<string> ToCSharpString<TValue>(
        IComponentContext context,
        TValue value,
        PartExtractor<TValue> extractor
    )
    {
        using var parts = new PartsBuilder();
        using var bag = PooledDiagnosticBag.Get();

        extractor(context, value, in parts, bag);

        if (bag.HasAny) return new(bag.ToCollection());

        return BuildCSharpString(context, in parts);
    }


    private static Result<string> BuildCSharpString(
        IComponentContext context,
        scoped ref readonly PartsBuilder parts
    )
    {
        if (parts.Count is 0) return "string.Empty";

        TrimLeadingAndTrailingTrivia(in parts);
        
        GetStringParameters(
            in parts,
            out var quoteCount,
            out var dollarCount,
            out var isMultiline
        );

        using var ____ = StringBuilder.Pooled(out var sb);

        int literalIndex = 0, interpolationIndex = 0;

        for (var i = 0; i < parts.Count; i++)
        {
            LexedCXTrivia leadingTrivia;
            LexedCXTrivia trailingTrivia;
            object part;

            if (parts.IsInterpolationAt(i))
            {
                var interpolation = parts.Interpolations[interpolationIndex++];
                leadingTrivia = interpolation.LeadingTrivia.WhitespaceOnly();
                trailingTrivia = interpolation.TrailingTrivia.WhitespaceOnly();
                part = interpolation.Value;
            }
            else
            {
                var literal = parts.Literals[literalIndex++];
                leadingTrivia = literal.LeadingTrivia.WhitespaceOnly();
                trailingTrivia = literal.TrailingTrivia.WhitespaceOnly();
                part = literal.Value;
            }
            
            sb.Append(leadingTrivia);

            switch (part)
            {
                case string str:
                    sb.Append(str);
                    break;
                case IInterpolationInfo info:
                    sb.Append('{', dollarCount);
                    sb.Append(context.GetReferenceToDesignerValue(info));
                    sb.Append('}', dollarCount);
                    break;

                default:
                    // something is really wrong
                    throw new InvalidOperationException(
                        $"Expected part to be a string or interpolation, but got {part.GetType().Name}"
                    );
            }

            sb.Append(trailingTrivia);
        }

        var innerStringValue = sb.ToString().NormalizeIndentation().Trim(['\r', '\n']);

        sb.Clear();

        if (parts.HasInterpolations && isMultiline)
            innerStringValue = innerStringValue.Indent(dollarCount);

        if (parts.HasInterpolations)
            sb.Append('$', dollarCount);

        sb.Append('"', quoteCount);

        if (isMultiline) sb.AppendLine();

        sb.Append(innerStringValue);

        if (isMultiline)
        {
            sb.AppendLine();
            
            if (parts.HasInterpolations)
                sb.Append(' ', dollarCount);
        }

        sb.Append('"', quoteCount);

        return sb.ToString();

        
        static void TrimLeadingAndTrailingTrivia(
            scoped ref readonly PartsBuilder parts
        )
        {
            if (parts.Count is 0) return;

            if (parts.IsInterpolationAt(0))
            {
                var leading = parts.Interpolations[0];
                TrimLeading(ref leading);
                parts.Interpolations[0] = leading;
            }
            else
            {
                var leading = parts.Literals[0];
                TrimLeading(ref leading);
                parts.Literals[0] = leading;
            }
            
            if (parts.IsInterpolationAt(parts.Count - 1))
            {
                var trailing = parts.Interpolations[parts.Interpolations.Count - 1];
                TrimTrailing(ref trailing);
                parts.Interpolations[parts.Interpolations.Count - 1] = trailing;
            }
            else
            {
                var trailing = parts.Literals[parts.Literals.Count - 1];
                TrimTrailing(ref trailing);
                parts.Literals[parts.Literals.Count - 1] = trailing;
            }

            static void TrimLeading<T>(ref ContainsTrivia<T> containsTrivia)
            {
                // try to remove trivia leading up to the first newline
                for (var j = 0; j < containsTrivia.LeadingTrivia.Count; j++)
                {
                    var trivia = containsTrivia.LeadingTrivia[j];

                    if (trivia is not CXTrivia.Token { Kind: CXTriviaTokenKind.Newline }) continue;

                    // remove all trivia leading up to the newline

                    containsTrivia = containsTrivia with
                    {
                        LeadingTrivia = containsTrivia.LeadingTrivia.RemoveRange(0, j + 1)
                    };

                    break;
                }
            }
            
            static void TrimTrailing<T>(ref ContainsTrivia<T> containsTrivia)
            {
                // try to remove trivia after the last newline
                for (var j = containsTrivia.TrailingTrivia.Count - 1; j >= 0; j--)
                {
                    var trivia = containsTrivia.TrailingTrivia[j];
                    if (trivia is not CXTrivia.Token { Kind: CXTriviaTokenKind.Newline }) continue;

                    // remove all trivia after the newline
                    containsTrivia = containsTrivia with
                    {
                        TrailingTrivia = containsTrivia
                            .TrailingTrivia
                            .RemoveRange(j, containsTrivia.TrailingTrivia.Count - j)
                    };
                    break;
                }
            }
        }

        static void GetStringParameters(
            scoped ref readonly PartsBuilder parts,
            out int quoteCount,
            out int dollarCount,
            out bool isMultiline
        )
        {
            quoteCount = 0;
            dollarCount = 0;
            isMultiline = false;

            char? last = null;
            var currentSequentialQuoteCount = 0;
            var currentSequentialBracketCount = 0;

            foreach (var part in parts.Literals)
            {
                isMultiline |= part.LeadingTrivia.ContainsNewlines || part.TrailingTrivia.ContainsNewlines;

                foreach (var ch in part.Value)
                {
                    switch (ch)
                    {
                        case '\n':
                            isMultiline = true;
                            break;
                        case '{' or '}':
                            if (last is null)
                            {
                                last = ch;
                                currentSequentialBracketCount = 1;
                                continue;
                            }

                            if (last == ch)
                            {
                                currentSequentialBracketCount++;
                                continue;
                            }

                            break;

                        case '"':
                            if (last is null)
                            {
                                last = ch;
                                currentSequentialQuoteCount = 1;
                                continue;
                            }

                            if (last == ch)
                            {
                                currentSequentialQuoteCount++;
                                continue;
                            }

                            break;
                    }

                    if (currentSequentialQuoteCount > 0)
                    {
                        quoteCount = Math.Max(quoteCount, currentSequentialQuoteCount);
                        currentSequentialQuoteCount = 0;
                    }

                    if (currentSequentialBracketCount > 0)
                    {
                        dollarCount = Math.Max(dollarCount, currentSequentialBracketCount);
                        currentSequentialBracketCount = 0;
                    }

                    last = null;
                }
            }

            // we must have more quotes than what has appeared, so add 1 to the final number
            quoteCount = Math.Max(quoteCount, currentSequentialQuoteCount) + 1;

            // multi-line string literals must have at least 3 quotes
            if (isMultiline)
                quoteCount = Math.Max(3, quoteCount);

            // can't have only 2 quotes for a string 
            else if (quoteCount is 2)
                quoteCount = 3;

            dollarCount = Math.Max(dollarCount, currentSequentialBracketCount) + 1;
        }
    }
}