using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Discord;

/// <summary>
///     Represents components built with the Component Designer.
/// </summary>
public sealed class CXMessageComponent :
    INestedComponent,
    IComponentContainer
{
    /// <summary>
    ///     An empty <see cref="CXMessageComponent"/> with no inner components.
    /// </summary>
    public static readonly CXMessageComponent Empty = new();

    /// <summary>
    ///     Gets whether this <see cref="CXMessageComponent"/> represents a single component.
    /// </summary>
    public bool IsSingle => Components.Count is 1;

    /// <summary>
    ///     Gets whether this <see cref="CXMessageComponent"/> contains no components.
    /// </summary>
    public bool IsEmpty => Components.Count is 0;

    /// <summary>
    ///     Gets a read-only list of built <see cref="IMessageComponent"/>s contained within this
    ///     <see cref="CXMessageComponent"/>.
    /// </summary>
    public IReadOnlyList<IMessageComponent> Components
        => _components ??= [..(_builders ??= []).Select(x => x.Build())];

    /// <summary>
    ///     Gets a read-only list of <see cref="IMessageComponentBuilder"/>s contained within this
    ///     <see cref="CXMessageComponent"/>.
    /// </summary>
    public IReadOnlyList<IMessageComponentBuilder> Builders
        => _builders ??= [..(_components ??= []).Select(x => x.ToBuilder())];

    private IReadOnlyList<IMessageComponentBuilder>? _builders;
    private IReadOnlyList<IMessageComponent>? _components;

    private MessageComponent? _built;

    /// <summary>
    ///     Constructs a new, empty <see cref="CXMessageComponent"/>.
    /// </summary>
    public CXMessageComponent()
    {
        _builders = [];
    }

    /// <summary>
    ///     Constructs a nes <see cref="CXMessageComponent"/> with the provided <see cref="IMessageComponentBuilder"/>s.
    /// </summary>
    /// <param name="components">The components for the newly constructed <see cref="CXMessageComponent"/>.</param>
    public CXMessageComponent(params IEnumerable<IMessageComponentBuilder> components)
    {
        _builders = [..components];
    }

    /// <summary>
    ///     Constructs a nes <see cref="CXMessageComponent"/> with the provided <see cref="IMessageComponent"/>s.
    /// </summary>
    /// <param name="components">The components for the newly constructed <see cref="CXMessageComponent"/>.</param>
    public CXMessageComponent(params IEnumerable<IMessageComponent> components)
    {
        _components = [..components];
    }

    /// <summary>
    ///     Constructs a nes <see cref="CXMessageComponent"/> with the provided <see cref="MessageComponent"/>
    /// </summary>
    /// <param name="component">
    ///     The <see cref="MessageComponent"/> containing the components for the newly constructed
    ///     <see cref="CXMessageComponent"/>.
    /// </param>
    public CXMessageComponent(MessageComponent component) : this(component.Components)
    {
    }

    /// <summary>
    ///     Converts this <see cref="CXMessageComponent"/> to the equivalent <see cref="MessageComponent"/>.
    /// </summary>
    public MessageComponent ToDiscordComponents()
        => _built ??= new ComponentBuilderV2(Components).Build();

    /// <summary>
    ///     Converts this <see cref="CXMessageComponent"/> to the equivalent <see cref="MessageComponent"/>.
    /// </summary>
    public static implicit operator MessageComponent(CXMessageComponent self)
        => self.ToDiscordComponents();

    /// <summary>
    ///     Converts a <see cref="MessageComponent"/> to the equivalent <see cref="CXMessageComponent"/>.
    /// </summary>
    public static implicit operator CXMessageComponent(MessageComponent component)
        => new(component);

    /// <summary>
    ///     Converts this <see cref="CXMessageComponent"/> to the equivalent <see cref="Optional{MessageComponent}"/>.
    /// </summary>
    /// <remarks>
    ///     The returned <see cref="Optional{MessageComponent}"/> will always have a value, regardless of if the current
    ///     <see cref="CXMessageComponent"/> contains any components.
    /// </remarks>
    public static implicit operator Optional<MessageComponent>(CXMessageComponent self)
        => self.ToDiscordComponents();

    /// <summary>
    ///     Creates a new <see cref="CXMessageComponent"/> containing the components from both the
    ///     <paramref name="left"/> and <paramref name="right"/> <see cref="CXMessageComponent"/>s.
    /// </summary>
    public static CXMessageComponent operator +(CXMessageComponent left, CXMessageComponent right)
        => new([..left.Builders, ..right.Builders]);

    /// <inheritdoc/>
    ImmutableArray<ComponentType> IComponentContainer.SupportedComponentTypes =>
    [
        ComponentType.ActionRow,
        ComponentType.Section,
        ComponentType.MediaGallery,
        ComponentType.Separator,
        ComponentType.Container,
        ComponentType.File,
        ComponentType.TextDisplay
    ];

    /// <inheritdoc/>
    int IComponentContainer.MaxChildCount => ComponentBuilderV2.MaxChildCount;

    /// <inheritdoc/>
    List<IMessageComponentBuilder> IComponentContainer.Components => [..Builders];

    /// <inheritdoc/>
    IComponentContainer IComponentContainer.AddComponent(IMessageComponentBuilder component)
        => new CXMessageComponent([..Builders, component]);

    /// <inheritdoc/>
    IComponentContainer IComponentContainer.AddComponents(params IMessageComponentBuilder[] components)
        => new CXMessageComponent([..Builders, ..components]);

    /// <inheritdoc/>
    IComponentContainer IComponentContainer.WithComponents(IEnumerable<IMessageComponentBuilder> components)
        => new CXMessageComponent(components);

    /// <inheritdoc/>
    IReadOnlyCollection<IMessageComponent> INestedComponent.Components => Components;
}