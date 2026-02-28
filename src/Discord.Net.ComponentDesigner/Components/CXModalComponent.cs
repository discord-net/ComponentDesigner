namespace Discord;

public sealed class CXModalComponent :
    CXComponent
{
    private ModalComponent? _built;
    
    public CXModalComponent() : base()
    {
    }

    public CXModalComponent(ModalComponent result) : this(result.Components)
    {
        _built = result;
    }

    public CXModalComponent(params IEnumerable<IMessageComponent> components) : base(components)
    {
    }

    public CXModalComponent(params IEnumerable<IMessageComponentBuilder> builders) : base(builders)
    {
    }

    public ModalComponent Build()
        => _built ??= new ModalComponentBuilder(Builders).Build();
}