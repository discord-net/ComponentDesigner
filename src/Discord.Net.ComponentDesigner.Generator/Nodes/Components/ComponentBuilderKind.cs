using System;
using System.Linq;
using Discord.Net.ComponentDesignerGenerator.Utils;
using Microsoft.CodeAnalysis;

namespace Discord.CX.Nodes.Components;

[Flags]
public enum ComponentBuilderKind
{
    None = 0,

    // ReSharper disable InconsistentNaming
    IMessageComponentBuilder = 0b001,
    IMessageComponent = 0b010,
    // ReSharper restore InconsistentNaming

    CXMessageComponent = 0b011,
    MessageComponent = 0b100,

    CollectionOf = 1 << 3,

    ComponentMask = IMessageComponentBuilder | IMessageComponent | CXMessageComponent | MessageComponent,

    CollectionOfIMessageComponentBuilders = IMessageComponentBuilder | CollectionOf,
    CollectionOfIMessageComponents = IMessageComponent | CollectionOf,
    CollectionOfCXComponents = CXMessageComponent | CollectionOf,
    CollectionOfMessageComponents = MessageComponent | CollectionOf,
}

public static class ComponentBuilderKindUtils
{
    public static bool SupportsCardinalityOfMany(this ComponentBuilderKind kind)
    {
        if (kind.HasFlag(ComponentBuilderKind.CollectionOf)) return true;

        return kind is ComponentBuilderKind.MessageComponent or ComponentBuilderKind.CXMessageComponent;
    }
    
    public static bool IsValidComponentBuilderType(
        ITypeSymbol? symbol,
        Compilation compilation,
        out ComponentBuilderKind kind
    )
    {
        kind = ComponentBuilderKind.None;

        if (symbol is null) return false;

        var current = symbol;

        var enumerableType = current
            .AllInterfaces
            .FirstOrDefault(x =>
                x.IsGenericType &&
                x.ConstructedFrom.Equals(
                    compilation.GetKnownTypes().IEnumerableOfTType!,
                    SymbolEqualityComparer.Default
                )
            );

        if (enumerableType is not null)
        {
            kind |= ComponentBuilderKind.CollectionOf;
            current = enumerableType.TypeArguments[0];
        }

        if (current.IsInTypeTree(compilation.GetKnownTypes().MessageComponentType))
            kind |= ComponentBuilderKind.MessageComponent;
        else if (current.IsInTypeTree(compilation.GetKnownTypes().IMessageComponentBuilderType))
            kind |= ComponentBuilderKind.IMessageComponentBuilder;
        else if(current.IsInTypeTree(compilation.GetKnownTypes().IMessageComponentType))
            kind |= ComponentBuilderKind.IMessageComponent;
        else if (current.IsInTypeTree(compilation.GetKnownTypes().CXMessageComponentType))
            kind |= ComponentBuilderKind.CXMessageComponent;
        
        return (kind & ComponentBuilderKind.ComponentMask) is not 0;
    }
    
    public static bool IsValidComponentBuilderType(
        ITypeSymbol? symbol,
        Compilation compilation
    ) => IsValidComponentBuilderType(symbol, compilation, out _);
    
    public static bool TryConvertBasic(string source, ComponentBuilderKind from, ComponentBuilderKind to, out string result)
        => (result = ConvertBasic(source, from, to)!) is not null;

    public static bool TryConvert(
        string source,
        ComponentBuilderKind from,
        ComponentBuilderKind to,
        out string result,
        bool spreadCollections = false
    ) => (result = Convert(source, from, to, spreadCollections)!) is not null;

    public static string? Convert(
        string source,
        ComponentBuilderKind from,
        ComponentBuilderKind to,
        bool spreadCollections = false
    )
    {
        if (from is ComponentBuilderKind.None || to is ComponentBuilderKind.None) return null;

        var fromCollection = from.HasFlag(ComponentBuilderKind.CollectionOf);
        var toCollection = to.HasFlag(ComponentBuilderKind.CollectionOf);

        var fromBasicKind = from & ComponentBuilderKind.ComponentMask;
        var toBasicKind = to & ComponentBuilderKind.ComponentMask;

        var spread = spreadCollections ? ".." : string.Empty;

        switch (fromCollection, toCollection)
        {
            case (false, false):
                return ConvertBasic(source, from, to);
            case (true, false):
            {
                var converter = ConvertBasic("x", fromBasicKind, toBasicKind);
                return converter is not null ? $"{source}.Select(x => {converter})" : null;
            }
            case (false, true):
            {
                switch (fromBasicKind, toBasicKind)
                {
                    case (
                        ComponentBuilderKind.MessageComponent or ComponentBuilderKind.CXMessageComponent,
                        ComponentBuilderKind.IMessageComponent
                        ):
                        return $"{spread}{source}.Components";
                    case (
                        ComponentBuilderKind.MessageComponent,
                        ComponentBuilderKind.IMessageComponentBuilder
                        ):
                        return $"{spread}{source}.Components.Select(x => x.ToBuilder())";
                    case (
                        ComponentBuilderKind.CXMessageComponent,
                        ComponentBuilderKind.IMessageComponentBuilder
                        ):
                        return $"{spread}{source}.Builders";
                    default:
                        var converter = ConvertBasic(source, fromBasicKind, toBasicKind);
                        return converter is not null
                            ? spreadCollections ? converter : $"[{converter}]"
                            : null;
                }
            }
            case (true, true):
                switch (fromBasicKind, toBasicKind)
                {
                    case (ComponentBuilderKind.MessageComponent or ComponentBuilderKind.CXMessageComponent, ComponentBuilderKind
                        .IMessageComponent):
                        return $"{source}.SelectMany(x => x.Components)";
                    case (ComponentBuilderKind.MessageComponent, ComponentBuilderKind.IMessageComponentBuilder):
                        return $"{source}.SelectMany(x => x.Components.Select(x => x.ToBuilder()))";
                    case (ComponentBuilderKind.CXMessageComponent, ComponentBuilderKind.IMessageComponentBuilder):
                        return $"{source}.SelectMany(x => x.Builders)";
                    default:
                        var converter = ConvertBasic("x", fromBasicKind, toBasicKind);
                        return converter is not null
                            ? $"{source}.Select(x => {converter})"
                            : null;
                }
        }
    }

    public static string? ConvertBasic(string source, ComponentBuilderKind from, ComponentBuilderKind to)
    {
        const string ComponentBuilderRef = "global::Discord.ComponentBuilderV2";
        const string CXComponentRef = "global::Discord.CXMessageComponent";

        switch (from, to)
        {
            case (ComponentBuilderKind.IMessageComponent, ComponentBuilderKind.IMessageComponent):
                return source;
            case (ComponentBuilderKind.IMessageComponent, ComponentBuilderKind.IMessageComponentBuilder):
                return $"{source}.ToBuilder()";
            case (ComponentBuilderKind.IMessageComponent, ComponentBuilderKind.MessageComponent):
                return $"new {ComponentBuilderRef}({source}).Build()";
            case (ComponentBuilderKind.IMessageComponent, ComponentBuilderKind.CXMessageComponent):
                return $"new {CXComponentRef}({source})";

            case (ComponentBuilderKind.MessageComponent, ComponentBuilderKind.IMessageComponent):
                // no way to convert to single here
                return null;
            case (ComponentBuilderKind.MessageComponent, ComponentBuilderKind.IMessageComponentBuilder):
                // no way to convert to single
                return null;
            case (ComponentBuilderKind.MessageComponent, ComponentBuilderKind.MessageComponent):
                return source;
            case (ComponentBuilderKind.MessageComponent, ComponentBuilderKind.CXMessageComponent):
                return $"new {CXComponentRef}({source})";

            case (ComponentBuilderKind.IMessageComponentBuilder, ComponentBuilderKind.IMessageComponent):
                return $"{source}.Build()";
            case (ComponentBuilderKind.IMessageComponentBuilder, ComponentBuilderKind.IMessageComponentBuilder):
                return source;
            case (ComponentBuilderKind.IMessageComponentBuilder, ComponentBuilderKind.MessageComponent):
                return $"new {ComponentBuilderRef}({source}).Build()";
            case (ComponentBuilderKind.IMessageComponentBuilder, ComponentBuilderKind.CXMessageComponent):
                return $"new {CXComponentRef}({source})";

            case (ComponentBuilderKind.CXMessageComponent, ComponentBuilderKind.IMessageComponent):
                // no way to convert to single here
                return null;
            case (ComponentBuilderKind.CXMessageComponent, ComponentBuilderKind.IMessageComponentBuilder):
                // no way to convert to single here
                return null;
            case (ComponentBuilderKind.CXMessageComponent, ComponentBuilderKind.MessageComponent):
                return $"{source}.ToDiscordComponents()";
            case (ComponentBuilderKind.CXMessageComponent, ComponentBuilderKind.CXMessageComponent):
                return source;

            default: return null;
        }
    }

    public static string ExtrapolateKindToBuilders(ComponentBuilderKind kind, string source)
    {
        switch (kind)
        {
            // case 1: standard builder, we do nothing to the source
            case ComponentBuilderKind.IMessageComponentBuilder: return source;

            case ComponentBuilderKind.CollectionOfIMessageComponentBuilders: return $"..{source}";

            case ComponentBuilderKind.CXMessageComponent:
            case ComponentBuilderKind.IMessageComponent: return $"..({source}).Components.Select(x => x.ToBuilder())";

            case ComponentBuilderKind.CollectionOfCXComponents:
            case ComponentBuilderKind.CollectionOfIMessageComponents:
                return $"..({source}).SelectMany(x => x.Components.Select(x => x.ToBuilder()))";

            default:
                throw new ArgumentOutOfRangeException(nameof(kind));
        }
    }
}