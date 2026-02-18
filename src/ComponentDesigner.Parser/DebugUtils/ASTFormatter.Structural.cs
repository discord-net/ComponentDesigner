
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using ComponentDesigner;

namespace ComponentDesigner.Parser;

public sealed record ASTNodeFormattingOptions(
    bool OmitEmptyCollections = true,
    bool OmitNulls = true,
    bool OmitTrivia = false,
    bool IncludeTokenValue = true
)
{
    public static readonly ASTNodeFormattingOptions Default = new();
}

public static partial class ASTFormatter
{
    public static string ToStructuralFormat(this ICXNode node, ASTNodeFormattingOptions? options = null)
    {
        options ??= ASTNodeFormattingOptions.Default;
        var sb = new StringBuilder();

        Write(node);

        return sb.ToString();

        void Write(ICXNode node, int depth = 0, int padding = 2)
        {
            switch (node)
            {
                case ICXCollection col:
                {
                    if (col.Count is 0)
                    {
                        if (!options.OmitEmptyCollections) sb.Append("[]");
                        break;
                    }

                    if (sb[sb.Length - 2] is ':')
                    {
                        sb.Length -= 2;
                        sb.Append("[")
                            .Append(col.Count.ToString().Aqua())
                            .Append("]")
                            .Append(": ".Grey());
                    }
                    
                    var spacer = string.Empty.PadLeft((depth + 1) * padding);

                    sb.AppendLine("[").Append(spacer);

                    for (var i = 0; i < col.Count; i++)
                    {
                        var obj = col[i];

                        sb.Append("[".Grey())
                            .Append(i.ToString().Aqua())
                            .Append("]: ".Grey());

                        WriteValue(obj, depth + 1, padding);

                        sb.AppendLine(",").Append(spacer);
                    }

                    sb.Length -= padding;
                    sb.Append("]");
                    break;
                }
                case CXToken token:
                {
                    sb.Append(token.Kind.ToString().Orange())
                        .Append("(".Orange())
                        .Append($"{token.TextSpan.Start}..{token.TextSpan.End}".Aqua());

                    if (token.Flags is not CXTokenFlags.None)
                    {
                        sb.Append(", ".Grey())
                            .Append(token.Flags.ToString().Violet());
                    }

                    if (options.IncludeTokenValue)
                    {
                        sb.Append(", ".Grey())
                            .Append(token.RawValue.Aqua());
                    }
                    
                    sb.Append(")".Orange());
                    
                    break;
                }
                default:
                {
                    var props = node
                        .GetType()
                        .GetProperties()
                        .Where(x => x.GetCustomAttribute<CompilerGeneratedAttribute>() is null)
                        .ToArray();

                    sb.Append(
                        GetName(node.GetType()).Magenta()
                    );

                    if (node is CXToken { Flags: not CXTokenFlags.None } token)
                    {
                        sb.Append("(".Magenta());
                        sb.Append(token.Flags.ToString().Yellow());
                        sb.Append(")".Magenta());
                    }

                    sb.AppendLine(" {");

                    for (var i = 0; i < props.Length; i++)
                    {
                        var prop = props[i];

                        if (
                            !prop.GetMethod!.IsPublic ||
                            prop.GetIndexParameters().Length is not 0 ||
                            prop.Name
                            is nameof(ICXNode.Parent)
                            or nameof(ICXNode.Document)
                            or nameof(ICXNode.Slots)
                            or nameof(CXNode.FirstTerminal)
                            or nameof(CXNode.LastTerminal)
                            or nameof(CXNode.Descendants)
                            or nameof(CXNode.Ancestors)
                            or "Offset"
                            or nameof(CXNode.Width)
                            or nameof(CXNode.GraphWidth)
                            or "Source"
                            or nameof(CXDocument.Tokens)
                            or nameof(CXDocument.StringTable)
                            or nameof(CXElement.IsFragment)
                            or nameof(CXElement.Identifier)
                            or nameof(ICXNode.HasErrors)
                            or nameof(ICXNode.FullTextSpan)
                            or nameof(CXValue.Multipart.HasInterpolations)
                        )
                        {
                            continue;
                        }

                        var val = prop.GetValue(node);

                        var name = prop.Name switch
                        {
                            nameof(ICXNode.LeadingTrivia) or
                                nameof(ICXNode.TrailingTrivia) or
                                nameof(ICXNode.Width) or
                                nameof(ICXNode.GraphWidth)
                                => prop.Name.Yellow(),
                            _ => prop.Name.Grey()
                        };

                        var startPos = sb.Length;

                        sb.Append(string.Empty.PadLeft((depth + 1) * padding)).Append($"{name}: ");

                        var preValuePos = sb.Length;
                
                        WriteValue(val, depth + 1, padding);

                        if (preValuePos == sb.Length)
                            sb.Length -= sb.Length - startPos;
                        else
                            sb.AppendLine(",");
                    }

                    sb.Append(string.Empty.PadLeft(depth * padding)).Append("}");
                    break;
                }
            }
        }

        void WriteValue(object? value, int depth, int padding)
        {
            if (value is null)
            {
                if (!options.OmitNulls) sb.Append("<null>");
                return;
            }

            switch (value)
            {
                case CXTrivia trivia:
                {
                    switch (trivia)
                    {
                        case CXTrivia.Token token:
                            sb.Append(token.Kind.ToString().Orange())
                                .Append("(".Orange())
                                .Append($"{token.Length}".Aqua())
                                .Append(")".Orange());
                            break;
                        case CXTrivia.XmlComment xmlComment:
                            sb.Append("XMLComment".Orange())
                                .Append("(".Orange())
                                .Append($"{xmlComment.Value.Value}".Aqua())
                                .Append(")".Orange());
                            break;
                    }
                    break;
                }
                case CXTextSpan span:
                    sb.Append($"{span.Start}..{span.End}".Aqua());
                    break;
                case ICXNode node:
                    Write(node, depth, padding);
                    break;
                case string str:
                    sb.Append($"'{str}'".Green());
                    break;
                case IEnumerable enumerable:
                    var col = enumerable.Cast<object>().ToArray();

                    if (col.Length is 0)
                    {
                        if (!options.OmitEmptyCollections) sb.Append("[]");
                        break;
                    }

                    if (sb[sb.Length - 2] is ':')
                    {
                        sb.Length -= 2;
                        sb.Append("[")
                            .Append(col.Length.ToString().Aqua())
                            .Append("]")
                            .Append(": ".Grey());
                    }
                    
                    var spacer = string.Empty.PadLeft((depth + 1) * padding);

                    sb.AppendLine("[").Append(spacer);

                    for (var i = 0; i < col.Length; i++)
                    {
                        var obj = col[i];

                        sb.Append("[".Grey())
                            .Append(i.ToString().Aqua())
                            .Append("]: ".Grey());

                        WriteValue(obj, depth + 1, padding);

                        sb.AppendLine(",").Append(spacer);
                    }

                    sb.Length -= padding;
                    sb.Append("]");
                    break;
                
                case var _ when value.GetType().IsValueType:
                    sb.Append(value.ToString()!.Aqua());
                    break;
                default:
                    sb.Append(value);
                    break;
            }
        }
    }

    private static string Grey(this string str)
        => ANSI(str, "1");
    
    private static string Green(this string str)
        => ANSI(str, "1;32");
    
    private static string Violet(this string str)
        => ANSI(str, "1;35");
    
    private static string Aqua(this string str)
        => ANSI(str, "1;36");
    
    private static string Magenta(this string str)
        => ANSI(str, "35");

    private static string Orange(this string str)
        => ANSI(str, "33");
    
    private static string Yellow(this string str)
        => ANSI(str, "1;33");

    private static string ANSI(this string str, string code)
        => $"\e[{code}m{str}\e[0m";

    private static string GetName(Type type)
    {
        var parts = new List<string>();

        var current = type;
        while (current is not null)
        {
            parts.Insert(0, current.Name);
            current = current.DeclaringType;
        }

        return string.Join(".", parts);
    }
}