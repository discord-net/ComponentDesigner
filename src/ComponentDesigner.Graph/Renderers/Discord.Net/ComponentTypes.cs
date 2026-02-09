using System.Runtime.CompilerServices;
using ComponentDesigner.Nodes;

namespace ComponentDesigner.Renderers.DiscordNet;

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

partial class DiscordNetComponentRenderer
{
    private static readonly ConditionalWeakTable<ICSharpTypeSymbol, ComponentSymbolInfo> ComponentSymbolInfos = new();

    private sealed record ComponentSymbolInfo(
        ComponentBuilderKind Kind,
        ICSharpTypeSymbol? Inner = null
    );

    public bool IsValidComponentType(
        IComponentContext context,
        ICSharpTypeSymbol? symbol,
        CancellationToken cancellationToken = default
    ) => symbol is not null && GetComponentSymbolInfo(context.CompilationProvider, symbol) is not null;

    private static ComponentSymbolInfo? GetComponentSymbolInfo(
        ICompilationProvider provider,
        ICSharpTypeSymbol symbol
    )
    {
        if (ComponentSymbolInfos.TryGetValue(symbol, out var info)) return info;
        
        var kind = ComponentBuilderKind.None;

        var current = symbol;
        ICSharpTypeSymbol? enumerableType = null;

        if (!current.Equals(provider.String))
            current.TryGetEnumerableType(out enumerableType);

        if (enumerableType is not null)
        {
            kind |= ComponentBuilderKind.CollectionOf;
            current = enumerableType;
        }

        if (current.Equals(MessageComponentType(provider)))
            kind |= ComponentBuilderKind.MessageComponent;
        else if (current.Equals(IMessageComponentBuilderType(provider)))
            kind |= ComponentBuilderKind.IMessageComponentBuilder;
        else if (current.Equals(IMessageComponentType(provider)))
            kind |= ComponentBuilderKind.IMessageComponent;
        else if (current.Equals(CXMessageComponentType(provider)))
            kind |= ComponentBuilderKind.CXMessageComponent;
        else if (current.Equals(CXModalComponentType(provider)))
            kind |= ComponentBuilderKind.CXModalComponent;
        else if (current.Equals(CXComponentType(provider)))
            kind |= ComponentBuilderKind.CXComponent;
        else if (current.Equals(ComponentBuilderV2Type(provider)))
            kind |= ComponentBuilderKind.ComponentBuilderV2;
        else if (current.Equals(ModalBuilderType(provider)))
            kind |= ComponentBuilderKind.ModalBuilder;

        var isComponent = (kind & ComponentBuilderKind.ComponentMask) is not ComponentBuilderKind.None;

        if (!isComponent) return null;

        info = new(kind, enumerableType);
        
        ComponentSymbolInfos.Add(symbol, info);

        return info;
    }
}

public static class ComponentBuilderKindExtensions
{
    extension(ComponentBuilderKind extKind)
    {
        public ComponentBuilderKind TypeOnly => extKind & ComponentBuilderKind.ComponentMask;

        public bool IsCollection => extKind.HasFlag(ComponentBuilderKind.CollectionOf);
    }
}