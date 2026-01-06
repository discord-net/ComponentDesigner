using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Discord.CX.Parser;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using SymbolDisplayFormat = Microsoft.CodeAnalysis.SymbolDisplayFormat;

namespace Discord.CX.Nodes.Components.Controls;

public sealed class MentionTextControlElement : TextControlElement
{
    private enum Kind
    {
        Unknown,
        User,
        Channel,
        Role,
        ApplicationCommand,
    }

    public override string FriendlyName => "Mention";

    public CXElement Element { get; }
    public CXValue Id { get; }
    public CXValue? Name { get; }

    private readonly Kind _kind;

    private MentionTextControlElement(
        CXElement element,
        Kind kind,
        CXValue id,
        CXValue? name
    ) : base(element)
    {
        Element = element;
        Id = id;
        Name = name;
        _kind = kind;
    }

    public static bool TryCreate(
        IComponentContext context,
        CXElement element,
        IList<DiagnosticInfo> diagnostics,
        List<CXToken> tokens,
        [MaybeNullWhen(false)] out MentionTextControlElement mention
    )
    {
        CXValue? id = null;
        CXValue? name = null;
        Kind? kind = null;

        if (!IsValidName(element.Identifier, ref kind))
        {
            mention = null;
            return false;
        }

        foreach (var attribute in element.Attributes)
        {
            switch (attribute.Identifier)
            {
                case "kind" or "type":
                    if (attribute.Value is null)
                    {
                        diagnostics.Add(
                            Diagnostics.MissingRequiredProperty(element.Identifier, attribute.Identifier),
                            attribute
                        );
                        continue;
                    }

                    if (!TryParseKind(context, attribute.Value, diagnostics, out var kindLocal))
                    {
                        // failure of kind ends our processing
                        mention = null;
                        return false;
                    }

                    if (kind is not null && kind != kindLocal)
                    {
                        diagnostics.Add(
                            Diagnostics.TypeMismatch(kind.ToString(), kindLocal.ToString()),
                            attribute
                        );

                        mention = null;
                        return false;
                    }

                    kind = kindLocal;
                    break;
                case "id":
                    if (id is not null)
                    {
                        diagnostics.Add(
                            Diagnostics.DuplicateProperty("id", "id"),
                            attribute
                        );
                        continue;
                    }

                    if (attribute.Value is null)
                    {
                        diagnostics.Add(
                            Diagnostics.MissingRequiredProperty(element.Identifier, attribute.Identifier),
                            attribute
                        );
                        continue;
                    }

                    id = attribute.Value;
                    break;
                case "name":
                    if (name is not null)
                    {
                        diagnostics.Add(
                            Diagnostics.DuplicateProperty("name", "name"),
                            attribute
                        );
                        continue;
                    }

                    if (attribute.Value is null)
                    {
                        diagnostics.Add(
                            Diagnostics.MissingRequiredProperty(element.Identifier, attribute.Identifier),
                            attribute
                        );
                        continue;
                    }

                    name = attribute.Value;
                    break;
                default:
                    diagnostics.Add(
                        Diagnostics.UnknownProperty(attribute.Identifier, element.Identifier),
                        attribute
                    );
                    break;
            }
        }

        ExtractChildValue();

        var isValid = true;


        if (kind is null)
        {
            diagnostics.Add(
                Diagnostics.MissingRequiredProperty(element.Identifier, "type"),
                element
            );

            isValid = false;
        }

        if (id is null)
        {
            diagnostics.Add(
                Diagnostics.MissingRequiredProperty(element.Identifier, "id"),
                element
            );

            isValid = false;
        }

        if (kind is Kind.ApplicationCommand && name is null)
        {
            diagnostics.Add(
                Diagnostics.MissingRequiredProperty(element.Identifier, "name"),
                element
            );

            isValid = false;
        }

        if (!isValid)
        {
            mention = null;
            return false;
        }

        if (
            id is not null &&
            name is not null &&
            !ReferenceEquals(id, name)
        )
        {
            // add in order
            if (id.Span.Start < name.Span.Start)
            {
                AddTokensFromValue(tokens, id);
                AddTokensFromValue(tokens, name);
            }
            else
            {
                AddTokensFromValue(tokens, name);
                AddTokensFromValue(tokens, id);
            }
        }
        else if (id is not null)
        {
            AddTokensFromValue(tokens, id);
        } 
        else if (name is not null)
        {
            AddTokensFromValue(tokens, name);
        }

        mention = new(
            element,
            kind!.Value,
            id!,
            name
        );
        return true;

        void ExtractChildValue()
        {
            if (element.Children.Count is 0) return;

            if (element.Children[0] is CXValue.Interpolation interpolation)
            {
                var info = context.GetInterpolationInfo(interpolation);

                Kind? inferredKind = null;

                if (
                    context.Compilation.HasImplicitConversion(
                        info.Symbol,
                        context.KnownTypes.IUserType
                    )
                )
                {
                    inferredKind = Kind.User;
                }
                else if (
                    context.Compilation.HasImplicitConversion(
                        info.Symbol,
                        context.KnownTypes.IChannelType
                    )
                )
                {
                    inferredKind = Kind.Channel;
                }
                else if (
                    context.Compilation.HasImplicitConversion(
                        info.Symbol,
                        context.KnownTypes.IRoleType
                    )
                )
                {
                    inferredKind = Kind.Role;
                }
                else if (
                    context.Compilation.HasImplicitConversion(
                        info.Symbol,
                        context.KnownTypes.IApplicationCommandType
                    )
                )
                {
                    inferredKind = Kind.ApplicationCommand;
                }

                if (inferredKind is not null)
                {
                    if (kind is not null && kind != inferredKind)
                    {
                        diagnostics.Add(
                            Diagnostics.TypeMismatch(kind.Value.ToString(), info.Symbol!.ToDisplayString()),
                            interpolation
                        );
                        return;
                    }

                    if (kind is null)
                    {
                        kind = inferredKind;
                        id = interpolation;
                        name = interpolation;
                        return;
                    }
                }
            }

            if (
                id is not null &&
                (kind is not Kind.ApplicationCommand || name is not null)
            )
            {
                // should not allow children
                diagnostics.Add(
                    Diagnostics.ComponentDoesntAllowChildren(element.Identifier),
                    element.Children
                );
                return;
            }

            if (element.Children[0] is not CXValue value)
            {
                diagnostics.Add(
                    Diagnostics.TypeMismatch("value", "element"),
                    element.Children
                );

                return;
            }

            if (id is null) id = value;
            else if (name is null && kind is Kind.ApplicationCommand) name = value;
            else return;

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

        static bool TryParseKind(
            IComponentContext context,
            CXValue value,
            IList<DiagnosticInfo> diagnostics,
            out Kind kind
        )
        {
            if (!value.TryGetConstantValue(context, out var constant))
            {
                diagnostics.Add(
                    Diagnostics.ExpectedAConstantValue,
                    value
                );
                kind = Kind.Unknown;
                return false;
            }

            kind = constant.ToLowerInvariant() switch
            {
                "user" => Kind.User,
                "role" => Kind.Role,
                "channel" => Kind.Channel,
                "command"
                    or "cmd"
                    or "slashcommand"
                    or "slash-command"
                    or "slashcmd"
                    or "slash-cmd"
                    or "applicationcmd"
                    or "app-cmd"
                    or "app-command"
                    or "application-command"
                    => Kind.ApplicationCommand,
                _ => Kind.Unknown
            };

            if (kind is Kind.Unknown)
            {
                diagnostics.Add(
                    Diagnostics.UnknownProperty(constant, "kind"),
                    value
                );

                return false;
            }

            return true;
        }

        static bool IsValidName(string name, ref Kind? kind)
        {
            switch (name)
            {
                case "mention": return true;

                case "user-mention":
                    kind = Kind.User;
                    return true;
                case "channel-mention":
                    kind = Kind.Channel;
                    return true;
                case "role-mention":
                    kind = Kind.Role;
                    return true;
                case "command-mention"
                    or "slash-mention"
                    or "slash-command-mention"
                    or "cmd-mention"
                    or "slash-cmd-mention"
                    or "app-cmd-mention"
                    or "application-cmd-mention"
                    or "application-command-mention":
                    kind = Kind.ApplicationCommand;
                    return true;
                default: return false;
            }
        }
    }

    protected override Result<RenderedTextControlElement> Render(
        IComponentContext context,
        TextControlRenderingOptions options
    ) => RenderId(context, options)
        .Combine(
            RenderName(context, options),
            Build
        );

    private Result<RenderedTextControlElement> Build(string id, string name)
    {
        var prefix = _kind switch
        {
            Kind.User => "@",
            Kind.Channel => "#",
            Kind.Role => "@&",
            Kind.ApplicationCommand => "/",
            _ => null
        };

        if (prefix is null) return Result<RenderedTextControlElement>.Empty;

        if (_kind is Kind.ApplicationCommand && string.IsNullOrWhiteSpace(name))
            return Result<RenderedTextControlElement>.Empty;

        var value = _kind is Kind.ApplicationCommand
            ? $"<{prefix}{name}:{id}>"
            : $"<{prefix}{id}>";

        return new RenderedTextControlElement(
            Element.LeadingTrivia,
            Element.TrailingTrivia,
            false,
            value
        );
    }

    private Result<string> RenderName(IComponentContext context, TextControlRenderingOptions options)
    {
        if (Name is null || _kind is not Kind.ApplicationCommand) return string.Empty;

        switch (Name)
        {
            case CXValue.Scalar scalar: return scalar.Value;
            case CXValue.Interpolation interpolation:
                return FromInterpolation(interpolation, context.GetInterpolationInfo(interpolation));

            case CXValue.Multipart multipart:
                if (!multipart.HasInterpolations)
                    return multipart.Tokens.ToValueString();
                if (multipart.IsLoneInterpolatedLiteral(context, out var info))
                    return FromInterpolation(multipart, info);

                goto default;
            default:
                return new DiagnosticInfo(
                    Diagnostics.TypeMismatch("string", Name.GetType().Name),
                    Name
                );
        }

        Result<string> FromInterpolation(ICXNode owner, DesignerInterpolationInfo info)
        {
            if (
                context.Compilation.HasImplicitConversion(
                    info.Symbol,
                    context.KnownTypes.IApplicationCommandType
                )
            )
            {
                return $"{options.StartInterpolation}{
                    context.GetDesignerValue(
                        info,
                        context.KnownTypes
                            .IApplicationCommandType!
                            .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                    )
                }.Name{options.EndInterpolation}";
            }

            if (info.Constant is { HasValue: true, Value: string str })
                return str;

            if (
                context.Compilation.HasImplicitConversion(
                    info.Symbol,
                    context.Compilation.GetSpecialType(SpecialType.System_String)
                )
            )
            {
                var conversion = info.Symbol?.SpecialType is SpecialType.System_String
                    ? string.Empty
                    : "(string)";

                return $"{options.StartInterpolation}{conversion}{
                    context.GetDesignerValue(
                        info
                    )
                }{options.EndInterpolation}";
            }

            return new DiagnosticInfo(
                Diagnostics.TypeMismatch("string", info.Symbol.ToDisplayString()),
                owner
            );
        }
    }

    private Result<string> RenderId(IComponentContext context, TextControlRenderingOptions options)
    {
        switch (Id)
        {
            case CXValue.Scalar scalar: return FromText(scalar, scalar.Value);
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
                    Diagnostics.TypeMismatch("snowflake", Id.GetType().Name),
                    Id
                );
        }

        Result<string> FromInterpolation(ICXNode owner, DesignerInterpolationInfo info)
        {
            switch (_kind)
            {
                case Kind.User when context.Compilation.HasImplicitConversion(
                    info.Symbol,
                    context.KnownTypes.IUserType
                ):
                case Kind.Channel when context.Compilation.HasImplicitConversion(
                    info.Symbol,
                    context.KnownTypes.IChannelType
                ):
                case Kind.Role when context.Compilation.HasImplicitConversion(
                    info.Symbol,
                    context.KnownTypes.IRoleType
                ):
                case Kind.ApplicationCommand when context.Compilation.HasImplicitConversion(
                    info.Symbol,
                    context.KnownTypes.IApplicationCommandType
                ):
                {
                    return $"{options.StartInterpolation}{
                        context.GetDesignerValue(
                            info,
                            info.Symbol!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                        )
                    }.Id{options.EndInterpolation}";
                }
            }

            if (info.Constant.HasValue)
            {
                switch (info.Constant.Value)
                {
                    case byte:
                    case sbyte and >= 0:
                    case ushort:
                    case short and >= 0:
                    case uint:
                    case >= 0:
                    case ulong:
                    case long and >= 0:
                        return info.Constant.Value.ToString();

                    case string str: return FromText(owner, str);
                }
            }

            if (
                context.Compilation.HasImplicitConversion(
                    info.Symbol,
                    context.Compilation.GetSpecialType(SpecialType.System_UInt64)
                )
            )
            {
                return $"{options.StartInterpolation}{
                    context.GetDesignerValue(
                        info,
                        context.Compilation
                            .GetSpecialType(SpecialType.System_UInt64)
                            .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                    )
                }{options.EndInterpolation}";
            }

            return new DiagnosticInfo(
                Diagnostics.TypeMismatch("snowflake", info.Symbol?.ToDisplayString() ?? "unknown"),
                owner
            );
        }

        Result<string> FromText(ICXNode owner, string text)
        {
            if (ulong.TryParse(text, out _)) return text;

            return new DiagnosticInfo(
                Diagnostics.TypeMismatch("snowflake", text),
                owner
            );
        }
    }
}