namespace Discord;

public sealed class CXComponent : CXComponent<CXComponent>
{
    public CXComponent() : base()
    {
    }

    public CXComponent(params IEnumerable<IMessageComponent> components) : base(components)
    {
    }

    public CXComponent(params IEnumerable<IMessageComponentBuilder> builders) : base(builders)
    {
    }
}

public abstract class CXComponent<TSelf> :
    ICXComponent<TSelf>
    where TSelf : CXComponent<TSelf>, new()
{
    public static TSelf Empty { get; } = new();

    public IReadOnlyList<IMessageComponentBuilder> Builders
        => _builders ??= [..(_components ??= []).Select(x => x.ToBuilder())];

    public IReadOnlyList<IMessageComponent> Components
        => _components ??= [..(_builders ??= []).Select(x => x.Build())];

    private IReadOnlyList<IMessageComponentBuilder>? _builders;
    private IReadOnlyList<IMessageComponent>? _components;

    protected CXComponent(params IEnumerable<IMessageComponent> components)
    {
        _components = [..components];
    }

    protected CXComponent(params IEnumerable<IMessageComponentBuilder> builders)
    {
        _builders = [..builders];
    }

    protected CXComponent()
    {
    }
}