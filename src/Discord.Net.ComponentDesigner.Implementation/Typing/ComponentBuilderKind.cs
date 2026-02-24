namespace Discord;

public enum ComponentBuilderKind
{
    None, 
    
    IMessageComponentBuilder,
    IMessageComponent,
    MessageComponent,
    ComponentBuilderV2,
    
    ModalComponent,
    ModalBuilder,
    
    CXComponent,
    CXMessageComponent,
    CXModalComponent,
}