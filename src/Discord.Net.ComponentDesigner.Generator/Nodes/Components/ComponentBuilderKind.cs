using System;
using System.Linq;
using Discord.CX.Parser;
using Microsoft.CodeAnalysis;

namespace Discord.CX.Nodes.Components;

// ReSharper disable InconsistentNaming
[Flags]
public enum ComponentBuilderKind
{
    None = 0,

    IMessageComponentBuilder = 0b001,
    IMessageComponent = 0b010,

    CXMessageComponent = 0b0011,
    CXModalComponent = 0b0100,
    CXComponent = 0b0101,
    MessageComponent = 0b0110,
    ModalComponent = 0b0111,
    ComponentBuilderV2 = 0b1000,
    ModalBuilder = 0b1001,

    CollectionOf = 1 << 4,

    ComponentMask = 0b1111,

    CollectionOfIMessageComponentBuilders = IMessageComponentBuilder | CollectionOf,
    CollectionOfIMessageComponents = IMessageComponent | CollectionOf,
    CollectionOfCXComponents = CXComponent | CollectionOf,
    CollectionOfCXMessageComponents = CXMessageComponent | CollectionOf,
    CollectionOfCXModalComponents = CXModalComponent | CollectionOf,
    CollectionOfMessageComponents = MessageComponent | CollectionOf,
    CollectionOfModalComponents = ModalComponent | CollectionOf,
    CollectionOfComponentBuilderV2 = ComponentBuilderV2 | CollectionOf,
    CollectionOfModalBuilder = ModalBuilder | CollectionOf,
}

public static class ComponentBuilderKindExtensions
{
    extension(ComponentBuilderKind extKind)
    {
        public ComponentBuilderKind TypeOnly => extKind & ComponentBuilderKind.ComponentMask;
        public bool IsCollection => extKind.HasFlag(ComponentBuilderKind.CollectionOf);

        public bool SupportsCardinalityOfMany
        {
            get
            {
                if (extKind.HasFlag(ComponentBuilderKind.CollectionOf)) return true;

                return extKind is ComponentBuilderKind.MessageComponent or ComponentBuilderKind.CXMessageComponent;
            }
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
            ITypeSymbol? enumerableType = null;

            if (current.SpecialType is not SpecialType.System_String)
                current.TryGetEnumerableType(out enumerableType);

            if (enumerableType is not null)
            {
                kind |= ComponentBuilderKind.CollectionOf;
                current = enumerableType;
            }

            var knownTypes = compilation.GetKnownTypes();

            if (current.IsInTypeTree(knownTypes.MessageComponentType))
                kind |= ComponentBuilderKind.MessageComponent;
            else if (current.IsInTypeTree(knownTypes.IMessageComponentBuilderType))
                kind |= ComponentBuilderKind.IMessageComponentBuilder;
            else if (current.IsInTypeTree(knownTypes.IMessageComponentType))
                kind |= ComponentBuilderKind.IMessageComponent;
            else if (current.IsInTypeTree(knownTypes.CXMessageComponentType))
                kind |= ComponentBuilderKind.CXMessageComponent;
            else if (current.IsInTypeTree(knownTypes.CXModalComponentType))
                kind |= ComponentBuilderKind.CXModalComponent;
            else if (current.IsInTypeTree(knownTypes.CXComponentType))
                kind |= ComponentBuilderKind.CXComponent;
            else if (current.IsInTypeTree(knownTypes.ComponentBuilderV2Type))
                kind |= ComponentBuilderKind.ComponentBuilderV2;
            else if (current.IsInTypeTree(knownTypes.ModalBuilderType))
                kind |= ComponentBuilderKind.ModalBuilder;

            return (kind & ComponentBuilderKind.ComponentMask) is not 0;
        }

        public static bool IsValidComponentBuilderType(
            ITypeSymbol? symbol,
            Compilation compilation
        ) => IsValidComponentBuilderType(symbol, compilation, out _);

        public Result<string> Convert(
            string source,
            ICXNode node,
            ComponentBuilderKind to,
            bool allowSpreads = false,
            bool allowConversionFromMessageToModalComponents = false,
            bool allowConversionFromModalToMessageComponents = false
        ) => extKind.Convert(
            new LocalSource(node.Span, source),
            to,
            allowSpreads,
            allowConversionFromMessageToModalComponents,
            allowConversionFromModalToMessageComponents
        );

        public Result<string> Convert(
            LocalSource source,
            ComponentBuilderKind to,
            bool allowSpreads = false,
            bool allowConversionFromMessageToModalComponents = false,
            bool allowConversionFromModalToMessageComponents = false
        ) => ComponentBuilderKindConverters.Convert(source, extKind, to);

        public Result<string> Conform(
            string source,
            ComponentTypingContext context,
            ICXNode node
        ) => Conform(source, extKind, context, node);


        public static Result<string> Conform(
            string code,
            ComponentBuilderKind kind,
            ComponentTypingContext typingContext,
            ICXNode source
        ) => kind.Convert(
            code,
            source,
            typingContext.ConformingType,
            typingContext.CanSplat
        );
    }
}