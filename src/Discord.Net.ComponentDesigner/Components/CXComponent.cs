namespace Discord;

public class CXComponent
{
    public bool IsSingle => Components.Count is 1;
    public bool IsEmpty => Components.Count is 0;
    
    public IReadOnlyList<IMessageComponentBuilder> Builders
        => _builders ??= [..(_components ??= []).Select(x => x.ToBuilder())];

    public IReadOnlyList<IMessageComponent> Components
        => _components ??= [..(_builders ??= []).Select(x => x.Build())];

    private IReadOnlyList<IMessageComponentBuilder>? _builders;
    private IReadOnlyList<IMessageComponent>? _components;
    
    public CXComponent(params IEnumerable<IMessageComponent> components)
    {
        _components = [..components];
    }

    public CXComponent(params IEnumerable<IMessageComponentBuilder> builders)
    {
        _builders = [..builders];
    }

    public CXComponent()
    {
    }
}