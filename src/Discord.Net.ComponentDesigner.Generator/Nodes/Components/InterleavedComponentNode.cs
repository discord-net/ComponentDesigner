using System;
using Discord.CX.Parser;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using SymbolDisplayFormat = Microsoft.CodeAnalysis.SymbolDisplayFormat;

namespace Discord.CX.Nodes.Components;

[Flags]
public enum InterleavedKind
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

public static class InterleavedKindExtensions
{
    public static bool SupportsCardinalityOfMany(this InterleavedKind kind)
    {
        if (kind.HasFlag(InterleavedKind.CollectionOf)) return true;

        return kind is InterleavedKind.MessageComponent or InterleavedKind.CXMessageComponent;
    }
}

public sealed class InterleavedState : ComponentState
{
    public required int InterpolationId { get; init; }
}

public sealed class InterleavedComponentNode : ComponentNode<InterleavedState>, IDynamicComponentNode
{
    public InterleavedKind Kind { get; }
    public ITypeSymbol Symbol { get; }

    public bool IsSingleCardinality
        => Kind == InterleavedKind.IMessageComponentBuilder;

    public override string Name => "<interpolated component>";

    public InterleavedComponentNode(
        InterleavedKind kind,
        ITypeSymbol symbol
    )
    {
        Kind = kind;
        Symbol = symbol;
    }

    public static bool IsValidInterleavedType(
        ITypeSymbol? symbol,
        Compilation compilation
    ) => IsValidInterleavedType(symbol, compilation, out _);

    public static bool IsValidInterleavedType(
        ITypeSymbol? symbol,
        Compilation compilation,
        out InterleavedKind kind
    )
    {
        kind = InterleavedKind.None;

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
            kind |= InterleavedKind.CollectionOf;
            current = enumerableType.TypeArguments[0];
        }

        if (
            compilation.HasImplicitConversion(
                current,
                compilation.GetKnownTypes().CXMessageComponentType
            )
        )
        {
            kind |= InterleavedKind.CXMessageComponent;
        }
        else if (
            compilation.HasImplicitConversion(
                current,
                compilation.GetKnownTypes().IMessageComponentBuilderType
            )
        )
        {
            kind |= InterleavedKind.IMessageComponentBuilder;
        }
        else if (
            compilation.HasImplicitConversion(
                current,
                compilation.GetKnownTypes().IMessageComponentType
            )
        )
        {
            kind |= InterleavedKind.IMessageComponent;
        }
        else if (
            compilation.HasImplicitConversion(
                current,
                compilation.GetKnownTypes().MessageComponentType
            )
        )
        {
            kind |= InterleavedKind.IMessageComponent;
        }

        return (kind & InterleavedKind.ComponentMask) is not 0;
    }

    public static bool TryCreate(
        ITypeSymbol? symbol,
        Compilation compilation,
        out InterleavedComponentNode node
    )
    {
        if (IsValidInterleavedType(symbol, compilation, out var kind))
        {
            node = new(kind, symbol!);
            return true;
        }

        node = null!;
        return false;
    }

    public static bool TryConvertBasic(string source, InterleavedKind from, InterleavedKind to, out string result)
        => (result = ConvertBasic(source, from, to)!) is not null;

    public static bool TryConvert(
        string source,
        InterleavedKind from,
        InterleavedKind to,
        out string result,
        bool spreadCollections = false
    ) => (result = Convert(source, from, to, spreadCollections)!) is not null;

    public static string? Convert(
        string source,
        InterleavedKind from,
        InterleavedKind to,
        bool spreadCollections = false
    )
    {
        if (from is InterleavedKind.None || to is InterleavedKind.None) return null;

        var fromCollection = from.HasFlag(InterleavedKind.CollectionOf);
        var toCollection = to.HasFlag(InterleavedKind.CollectionOf);

        var fromBasicKind = from & InterleavedKind.ComponentMask;
        var toBasicKind = to & InterleavedKind.ComponentMask;

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
                        InterleavedKind.MessageComponent or InterleavedKind.CXMessageComponent,
                        InterleavedKind.IMessageComponent
                        ):
                        return $"{spread}{source}.Components";
                    case (
                        InterleavedKind.MessageComponent,
                        InterleavedKind.IMessageComponentBuilder
                        ):
                        return $"{spread}{source}.Components.Select(x => x.ToBuilder())";
                    case (
                        InterleavedKind.CXMessageComponent,
                        InterleavedKind.IMessageComponentBuilder
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
                    case (InterleavedKind.MessageComponent or InterleavedKind.CXMessageComponent, InterleavedKind
                        .IMessageComponent):
                        return $"{source}.SelectMany(x => x.Components)";
                    case (InterleavedKind.MessageComponent, InterleavedKind.IMessageComponentBuilder):
                        return $"{source}.SelectMany(x => x.Components.Select(x => x.ToBuilder()))";
                    case (InterleavedKind.CXMessageComponent, InterleavedKind.IMessageComponentBuilder):
                        return $"{source}.SelectMany(x => x.Builders)";
                    default:
                        var converter = ConvertBasic("x", fromBasicKind, toBasicKind);
                        return converter is not null
                            ? $"{source}.Select(x => {converter})"
                            : null;
                }
        }
    }

    public static string? ConvertBasic(string source, InterleavedKind from, InterleavedKind to)
    {
        const string ComponentBuilderRef = "global::Discord.ComponentBuilderV2";
        const string CXComponentRef = "global::Discord.CXMessageComponent";

        switch (from, to)
        {
            case (InterleavedKind.IMessageComponent, InterleavedKind.IMessageComponent):
                return source;
            case (InterleavedKind.IMessageComponent, InterleavedKind.IMessageComponentBuilder):
                return $"{source}.ToBuilder()";
            case (InterleavedKind.IMessageComponent, InterleavedKind.MessageComponent):
                return $"new {ComponentBuilderRef}({source}).Build()";
            case (InterleavedKind.IMessageComponent, InterleavedKind.CXMessageComponent):
                return $"new {CXComponentRef}({source})";

            case (InterleavedKind.MessageComponent, InterleavedKind.IMessageComponent):
                // no way to convert to single here
                return null;
            case (InterleavedKind.MessageComponent, InterleavedKind.IMessageComponentBuilder):
                // no way to convert to single
                return null;
            case (InterleavedKind.MessageComponent, InterleavedKind.MessageComponent):
                return source;
            case (InterleavedKind.MessageComponent, InterleavedKind.CXMessageComponent):
                return $"new {CXComponentRef}({source})";

            case (InterleavedKind.IMessageComponentBuilder, InterleavedKind.IMessageComponent):
                return $"{source}.Build()";
            case (InterleavedKind.IMessageComponentBuilder, InterleavedKind.IMessageComponentBuilder):
                return source;
            case (InterleavedKind.IMessageComponentBuilder, InterleavedKind.MessageComponent):
                return $"new {ComponentBuilderRef}({source}).Build()";
            case (InterleavedKind.IMessageComponentBuilder, InterleavedKind.CXMessageComponent):
                return $"new {CXComponentRef}({source})";

            case (InterleavedKind.CXMessageComponent, InterleavedKind.IMessageComponent):
                // no way to convert to single here
                return null;
            case (InterleavedKind.CXMessageComponent, InterleavedKind.IMessageComponentBuilder):
                // no way to convert to single here
                return null;
            case (InterleavedKind.CXMessageComponent, InterleavedKind.MessageComponent):
                return $"{source}.ToDiscordComponents()";
            case (InterleavedKind.CXMessageComponent, InterleavedKind.CXMessageComponent):
                return source;

            default: return null;
        }
    }

    public static string ExtrapolateKindToBuilders(InterleavedKind kind, string source)
    {
        switch (kind)
        {
            // case 1: standard builder, we do nothing to the source
            case InterleavedKind.IMessageComponentBuilder: return source;

            case InterleavedKind.CollectionOfIMessageComponentBuilders: return $"..{source}";

            case InterleavedKind.CXMessageComponent:
            case InterleavedKind.IMessageComponent: return $"..({source}).Components.Select(x => x.ToBuilder())";

            case InterleavedKind.CollectionOfCXComponents:
            case InterleavedKind.CollectionOfIMessageComponents:
                return $"..({source}).SelectMany(x => x.Components.Select(x => x.ToBuilder()))";

            default:
                throw new ArgumentOutOfRangeException(nameof(kind));
        }
    }

    public override InterleavedState? CreateState(ComponentStateInitializationContext context)
    {
        int id;

        switch (context.Node)
        {
            case CXValue.Interpolation interpolation:
                id = interpolation.Document.GetInterpolationIndex(interpolation.Token);
                break;
            case CXToken { Kind: CXTokenKind.Interpolation } token:
                id = token.Document!.GetInterpolationIndex(token);
                break;
            default: return null;
        }

        return new InterleavedState()
        {
            InterpolationId = id,
            Source = context.Node
        };
    }


    // TODO: extrapolate the kind to correct buidler conversion
    public override string Render(InterleavedState state, IComponentContext context)
        => context.GetDesignerValue(
            state.InterpolationId,
            context.KnownTypes.IMessageComponentBuilderType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
        );
}