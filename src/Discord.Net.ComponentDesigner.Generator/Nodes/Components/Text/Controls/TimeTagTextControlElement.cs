using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Discord.CX.Parser;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Discord.CX.Nodes.Components.Controls;

public sealed class TimeTagTextControlElement : TextControlElement
{
    public CXElement Element { get; }
    public CXValue Value { get; }
    public CXValue? Style { get; }
    public override string FriendlyName => "Timestamp";

    private TimeTagTextControlElement(
        CXElement element,
        CXValue value,
        CXValue? style
    ) : base(element)
    {
        Element = element;
        Value = value;
        Style = style;
    }

    public static bool TryCreate(
        IComponentContext context,
        CXElement element,
        IList<DiagnosticInfo> diagnostics,
        List<CXToken> tokens,
        [MaybeNullWhen(false)] out TimeTagTextControlElement tag
    )
    {
        CXValue? value = null;
        CXValue? style = null;

        foreach (var attribute in element.Attributes)
        {
            switch (attribute.Identifier)
            {
                case "style":
                    if (attribute.Value is null or CXValue.Invalid)
                    {
                        diagnostics.Add(
                            Diagnostics.MissingRequiredProperty(element.Identifier, "style"),
                            attribute
                        );
                        continue;
                    }

                    style = attribute.Value;
                    break;
                case "value" or "unix":
                    if (value is not null)
                    {
                        // duplicate
                        diagnostics.Add(
                            Diagnostics.DuplicateProperty("value", "unix"),
                            attribute
                        );
                        continue;
                    }
                    
                    if (attribute.Value is null or CXValue.Invalid)
                    {
                        diagnostics.Add(
                            Diagnostics.MissingRequiredProperty(element.Identifier, "value"),
                            attribute
                        );
                        continue;
                    }

                    value = attribute.Value;
                    break;
                default:
                    diagnostics.Add(
                        Diagnostics.UnknownProperty(attribute.Identifier, element.Identifier),
                        attribute
                    );
                    break;
            }
        }

        ExtractChildValue(ref value);

        if (value is null)
        {
            diagnostics.Add(
                Diagnostics.MissingRequiredProperty(element.Identifier, "value"),
                element
            );

            tag = null;
            return false;
        }

        // IMPORTANT: add the tokens in order of appearance
        if (style is not null)
        {
            if (style.Span.Start < value.Span.Start)
            {
                AddTokensFromValue(tokens, style);
                AddTokensFromValue(tokens, value);
            }
            else
            {
                AddTokensFromValue(tokens, value);
                AddTokensFromValue(tokens, style);
            }
        }
        else
        {
            AddTokensFromValue(tokens, value);
        }

        tag = new(element, value, style);
        return true;


        void ExtractChildValue(ref CXValue? value)
        {
            if (element.Children.Count is 0) return;

            if (value is not null)
            {
                diagnostics.Add(
                    Diagnostics.DuplicateProperty("value", "children"),
                    element.Children
                );

                return;
            }

            if (element.Children[0] is not CXValue childValue)
            {
                diagnostics.Add(
                    Diagnostics.InvalidChild(element.Identifier, element.Children[0].GetType().Name),
                    element.Children
                );

                return;
            }

            value = childValue;

            if (element.Children.Count > 1)
            {
                diagnostics.Add(
                    Diagnostics.TooManyChildren(element.Identifier),
                    CXTextSpan.FromBounds(
                        element.Children[1].Span.Start,
                        element.Children[element.Children.Count - 1].Span.End
                    )
                );
            }
        }
    }


    protected override Result<RenderedTextControlElement> Render(
        IComponentContext context,
        TextControlRenderingOptions options
    ) => RenderValue(context, Value, options)
        .Combine(
            RenderStyle(context, Style, options),
            Build
        );

    private RenderedTextControlElement Build(string value, string style)
        => new(
            Element.LeadingTrivia,
            Element.TrailingTrivia,
            false,
            string.IsNullOrEmpty(style)
                ? $"<t:{value}>"
                : $"<t:{value}:{style}>"
        );

    private static Result<string> RenderStyle(
        IComponentContext context,
        CXValue? style,
        TextControlRenderingOptions options
    )
    {
        if (style is null) return string.Empty;

        switch (style)
        {
            case CXValue.Scalar scalar:
                return FromText(scalar, scalar.Value);
            case CXValue.Interpolation interpolation:
                return FromInterpolation(interpolation, context.GetInterpolationInfo(interpolation));
            case CXValue.Multipart multipart:
                if (!multipart.HasInterpolations)
                    return FromText(multipart, multipart.Tokens.ToValueString());
                if (multipart.IsLoneInterpolatedLiteral(context, out var info))
                    return FromInterpolation(multipart, info);

                goto default;
            default:
                return new DiagnosticInfo(
                    Diagnostics.TypeMismatch("style", style.GetType().Name),
                    style
                );
        }

        Result<string> FromInterpolation(
            ICXNode owner,
            DesignerInterpolationInfo info
        )
        {
            if (info.Constant is { HasValue: true, Value: string str })
                return FromText(owner, str);

            if (
                context.Compilation.HasImplicitConversion(
                    info.Symbol,
                    context.KnownTypes.TimestampTagStylesEnum
                )
            )
            {
                return $"{options.StartInterpolation}(char){
                    context.GetDesignerValue(
                        info,
                        context.KnownTypes.TimestampTagStylesEnum!.ToDisplayString(
                            SymbolDisplayFormat.FullyQualifiedFormat
                        )
                    )
                }{options.EndInterpolation}";
            }

            return new DiagnosticInfo(
                Diagnostics.TypeMismatch(
                    context.KnownTypes.TimestampTagStylesEnum?.Name ?? "TimestampTagStyles",
                    info.Symbol?.ToDisplayString() ?? "unknown"
                ),
                style
            );
        }

        Result<string> FromText(ICXNode owner, string text)
        {
            if (text is "t" or "T" or "d" or "D" or "f" or "F" or "s" or "S" or "R") return text;

            // check enum names
            return text.ToLowerInvariant() switch
            {
                "shorttime" => "t",
                "longtime" => "T",
                "shortdate" => "d",
                "longdate" => "D",
                "shortdatetime" => "f",
                "longdatetime" => "F",
                "relative" => "R",
                _ => new DiagnosticInfo(
                    Diagnostics.TypeMismatch("timestamp style", "text"),
                    owner
                )
            };
        }
    }

    private static Result<string> RenderValue(
        IComponentContext context,
        CXValue value,
        TextControlRenderingOptions options
    )
    {
        switch (value)
        {
            case CXValue.Interpolation interpolation:
                return FromInterpolation(interpolation, context.GetInterpolationInfo(interpolation));
            case CXValue.Scalar scalar:
                return FromText(scalar, scalar.Value);
            case CXValue.Multipart multipart:
                if (!multipart.HasInterpolations)
                    return FromText(multipart, multipart.Tokens.ToValueString());

                if (multipart.IsLoneInterpolatedLiteral(context, out var info))
                    return FromInterpolation(multipart, info);

                goto default;
            default:
                return new DiagnosticInfo(
                    Diagnostics.TypeMismatch("date/seconds", value.GetType().Name),
                    value
                );
        }

        Result<string> FromText(ICXNode owner, string text)
        {
            if (DateTimeOffset.TryParse(text, out var dto))
                return dto.ToUnixTimeSeconds().ToString();

            if (DateTime.TryParse(text, out var dt))
                return ((DateTimeOffset)dt).ToUnixTimeSeconds().ToString();

            if (BigInteger.TryParse(text, out var _))
                return text;

            return new DiagnosticInfo(
                Diagnostics.TypeMismatch("date/seconds", "text"),
                owner
            );
        }

        Result<string> FromInterpolation(ICXNode owner, DesignerInterpolationInfo info)
        {
            if (
                context.Compilation.HasImplicitConversion(
                    info.Symbol,
                    context.KnownTypes.DateTimeOffsetType
                )
            )
            {
                return $"{options.StartInterpolation}{
                    context.GetDesignerValue(info, context.KnownTypes.DateTimeOffsetType!.ToDisplayString())
                }.ToUnixTimeSeconds(){options.EndInterpolation}";
            }

            if (info.Constant.HasValue)
            {
                switch (info.Constant.Value)
                {
                    // check for seconds-like value
                    case byte:
                    case sbyte:
                    case ushort:
                    case short:
                    case uint:
                    case int:
                    case ulong:
                    case long:
                        return info.Constant.Value.ToString();
                    case string str: return FromText(owner, str);
                }
            }

            if (
                context.Compilation.HasImplicitConversion(
                    info.Symbol,
                    context.Compilation.GetSpecialType(SpecialType.System_UInt64)
                ) ||
                context.Compilation.HasImplicitConversion(
                    info.Symbol,
                    context.Compilation.GetSpecialType(SpecialType.System_Int64)
                )
            )
            {
                return context.GetDesignerValue(info,
                    info.Symbol!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            }

            return new DiagnosticInfo(
                Diagnostics.TypeMismatch("date/seconds", info.Symbol?.ToDisplayString() ?? "unknown"),
                owner
            );
        }
    }
}