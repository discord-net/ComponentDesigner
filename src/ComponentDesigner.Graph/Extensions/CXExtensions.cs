using System.Diagnostics.CodeAnalysis;
using System.Text;
using ComponentDesigner.Nodes;
using ComponentDesigner.Parser;
using ComponentDesigner.Parser.Util;

namespace ComponentDesigner;

public static class CXExtensions
{
    public static bool TryGetSingleInterpolation(
        this CXValue.Multipart multipart,
        IComponentContext context,
        [MaybeNullWhen(false)] out IInterpolationInfo info
    )
    {
        if (multipart is { HasInterpolations: true, Tokens.Count: 1 } &&
            multipart.Tokens[0].InterpolationIndex is { } index)
        {
            info = context.GetInterpolationInfo(index);
            return true;
        }

        info = null;
        return false;
    }

    public static bool TryGetLiteralValue(
        this CXValue value,
        IComponentContext context,
        [MaybeNullWhen(false)] out string literal
    )
    {
        switch (value)
        {
            case CXValue.Scalar scalar:
                literal = scalar.Value;
                return true;
            case CXValue.Interpolation interpolation
                when TryGetInterpolationLiteral(context.GetInterpolationInfo(interpolation), out literal):
                return true;

            case CXValue.Multipart multipart:
                if (!multipart.HasInterpolations)
                {
                    literal = multipart.Tokens.ToValueString();
                    return true;
                }

                var sb = ObjectPool<StringBuilder>.Get();
                sb.Clear();

                foreach (var token in multipart.Tokens)
                {
                    if (token.InterpolationIndex is { } index)
                    {
                        if (!TryGetInterpolationLiteral(context.GetInterpolationInfo(index), out var part))
                            goto default;

                        sb.Append(part);
                    }
                    else
                    {
                        sb.Append(token.Value);
                    }
                }

                literal = sb.ToString();
                ObjectPool<StringBuilder>.Return(sb);
                return true;
            default:
                literal = null;
                return false;
        }

        static bool TryGetInterpolationLiteral(IInterpolationInfo info, [MaybeNullWhen(false)] out string literal)
        {
            if (info.ConstantValue is { IsSpecified: true, Value: string value })
            {
                literal = value;
                return true;
            }

            literal = null;
            return false;
        }
    }

    extension(CXElement element)
    {
        public CXTextSpan IdentifierTextSpanOrElementTextSpan
            => element is { OpeningTag.Identifier: { } identifier } ? identifier.TextSpan : element.TextSpan;
    }
}