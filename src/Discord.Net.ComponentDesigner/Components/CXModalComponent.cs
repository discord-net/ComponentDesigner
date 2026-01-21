namespace Discord;

public sealed class CXModalComponent : 
    CXComponent<CXModalComponent>,
    IBuildableCXComponent<CXModalComponent, ModalComponent>
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

    public static CXModalComponent From(ModalComponent result)
        => new(result);

    public static implicit operator ModalComponent(CXModalComponent self)
        => self.Build();

    public static implicit operator CXModalComponent(ModalComponent result)
        => From(result);
}