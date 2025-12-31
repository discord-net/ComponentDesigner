using System;
using System.Linq;
using System.Text;
using Discord.CX.Parser;

namespace Discord.CX.Nodes;

public sealed class StringGenerator : CXValueGenerator
{
    protected override Result<string> RenderInterpolation(
        IComponentContext context,
        CXValueGeneratorTarget target,
        CXToken token,
        DesignerInterpolationInfo info,
        CXValueGeneratorOptions options
    )
    {
        if (info.Constant.HasValue) return info.Constant.Value?.ToString() ?? string.Empty;

        return context.GetDesignerValue(info);
    }

    protected override Result<string> RenderMultipart(
        IComponentContext context,
        CXValueGeneratorTarget target,
        CXValue.Multipart multipart,
        CXValueGeneratorOptions options
    ) => ToCSharpString(multipart);

    protected override Result<string> RenderScalar(
        IComponentContext context,
        CXValueGeneratorTarget target,
        CXToken token,
        CXValueGeneratorOptions options
    ) => ToCSharpString(token.Value);
    
    public static string ToCSharpString(CXValue.Multipart literal, CXValueGeneratorOptions? options = null)
    {
        if (literal.Tokens.Count is 0) return "string.Empty";

        var sb = new StringBuilder();

        var literalParts = literal.Tokens
            .Where(x => x.Kind is CXTokenKind.Text)
            .Select(x => x.Value)
            .ToArray();

        if (literalParts.Length > 0)
        {
            literalParts[0] = literalParts[0].TrimStart();

            literalParts[literalParts.Length - 1] = literalParts[literalParts.Length - 1].TrimEnd();
        }

        var quoteCount = literalParts.Length is 0
            ? 1
            : literalParts.Select(x => x.Count(x => x is '"')).Max() + 1;

        var hasInterpolations = literal.Tokens.Any(x => x.Kind is CXTokenKind.Interpolation);

        var dollars = hasInterpolations
            ? new string(
                '$',
                literalParts.Length is 0
                    ? 1
                    : Math.Max(1, literalParts.Select(GetInterpolationDollarRequirement).Max())
            )
            : string.Empty;

        var startInterpolation = dollars.Length > 0
            ? new string('{', dollars.Length)
            : string.Empty;

        var endInterpolation = dollars.Length > 0
            ? new string('}', dollars.Length)
            : string.Empty;

        var isMultiline = false;

        for (var i = 0; i < literal.Tokens.Count; i++)
        {
            var token = literal.Tokens[i];

            // first and last token allow one newline before/after as syntax trivia
            var leadingTrivia = token.LeadingTrivia;
            var trailingTrivia = token.TrailingTrivia;

            for (var j = 0; j < leadingTrivia.Count; j++)
            {
                var trivia = leadingTrivia[j];
                if (trivia is not CXTrivia.Token { Kind: CXTriviaTokenKind.Newline }) continue;

                if (i != 0) continue;

                // remove all trivia leading up to this newline
                leadingTrivia = leadingTrivia.RemoveRange(0, j + 1);
                break;
            }

            for (var j = trailingTrivia.Count - 1; j >= 0; j--)
            {
                var trivia = trailingTrivia[j];
                if (trivia is not CXTrivia.Token { Kind: CXTriviaTokenKind.Newline }) continue;

                if (i != literal.Tokens.Count - 1) continue;

                // remove all trivia after the newline
                trailingTrivia = trailingTrivia.RemoveRange(j, trailingTrivia.Count - j);
                break;
            }

            isMultiline |=
            (
                trailingTrivia.ContainsNewlines ||
                leadingTrivia.ContainsNewlines ||
                token.Value.Contains("\n")
            );

            switch (token.Kind)
            {
                case CXTokenKind.Text:
                    sb
                        .Append(leadingTrivia)
                        .Append(EscapeBackslashes(token.Value))
                        .Append(trailingTrivia);
                    break;
                case CXTokenKind.Interpolation:
                    var index = literal.Document!.InterpolationTokens.IndexOf(token);

                    // TODO: handle better
                    if (index is -1) throw new InvalidOperationException();

                    sb
                        .Append(leadingTrivia)
                        .Append(startInterpolation)
                        .Append($"designer.GetValueAsString({index})")
                        .Append(endInterpolation)
                        .Append(trailingTrivia);
                    break;

                default: continue;
            }
        }

        // normalize the value indentation
        var value = sb.ToString().NormalizeIndentation().Trim(['\r', '\n']);

        // pad the value to the amount of dollar signs we have to properly align the value text to the 
        // multi-line string literal
        if (hasInterpolations && isMultiline)
            value = value.Indent(dollars.Length);

        sb.Clear();

        if (isMultiline)
        {
            sb.AppendLine();
            quoteCount = Math.Max(quoteCount, 3);
        }

        var quotes = new string('"', quoteCount);

        sb.Append(dollars).Append(quotes);

        if (isMultiline) sb.AppendLine();

        sb.Append(value);

        // ending quotes are on a different line 
        if (isMultiline) sb.AppendLine();

        // if it has interpolations, offset the ending quotes by the amount of dollar signs
        if (hasInterpolations && isMultiline) sb.Append("".PadLeft(dollars.Length));
        sb.Append(quotes);

        return sb.ToString();
    }

    public static int GetInterpolationDollarRequirement(string part)
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

        return result;
    }

    public static string ToCSharpString(string text)
    {
        var quoteCount = (GetSequentialQuoteCount(text) + 1) switch
        {
            2 => 3,
            var r => r
        };

        text = text.NormalizeIndentation().Trim(['\r', '\n']);

        var isMultiline = text.Contains('\n');

        if (isMultiline)
            quoteCount = Math.Max(3, quoteCount);

        var quotes = new string('"', quoteCount);

        var sb = new StringBuilder();

        if (isMultiline) sb.AppendLine();

        sb.Append(quotes);

        if (isMultiline) sb.AppendLine();

        sb.Append(text);

        if (isMultiline)
            sb.AppendLine();

        sb.Append(quotes);

        return sb.ToString();
    }

    private static string EscapeBackslashes(string text)
        => text.Replace("\\", @"\\");

    public static int GetSequentialQuoteCount(string text)
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

        return result;
    }
}