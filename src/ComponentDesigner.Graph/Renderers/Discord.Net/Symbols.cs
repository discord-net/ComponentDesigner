namespace ComponentDesigner.Renderers.DiscordNet;

partial class DiscordNetComponentRenderer
{
    public static ICSharpTypeSymbol? MessageComponentType(ICompilationProvider provider)
        => provider.GetTypeFromQualifiedName("Discord.MessageComponent");
    
    public static ICSharpTypeSymbol? IMessageComponentBuilderType(ICompilationProvider provider)
        => provider.GetTypeFromQualifiedName("Discord.IMessageComponentBuilder");
    
    public static ICSharpTypeSymbol? IMessageComponentType(ICompilationProvider provider)
        => provider.GetTypeFromQualifiedName("Discord.IMessageComponent");
    
    public static ICSharpTypeSymbol? CXMessageComponentType(ICompilationProvider provider)
        => provider.GetTypeFromQualifiedName("Discord.CXMessageComponent");
    
    public static ICSharpTypeSymbol? CXModalComponentType(ICompilationProvider provider)
        => provider.GetTypeFromQualifiedName("Discord.CXModalComponent");
    
    public static ICSharpTypeSymbol? CXComponentType(ICompilationProvider provider)
        => provider.GetTypeFromQualifiedName("Discord.CXComponent");
    
    public static ICSharpTypeSymbol? ComponentBuilderV2Type(ICompilationProvider provider)
        => provider.GetTypeFromQualifiedName("Discord.ComponentBuilderV2");
    
    public static ICSharpTypeSymbol? ModalBuilderType(ICompilationProvider provider)
        => provider.GetTypeFromQualifiedName("Discord.ModalBuilder");
    
    public static Result<ICSharpTypeSymbol> ContainerBuilder(CXTextSpan textSpan, ICompilationProvider provider)
        => GetSymbol(textSpan, provider, "Discord.ContainerBuilder");
    
    public static Result<ICSharpTypeSymbol> TextDisplayBuilder(CXTextSpan textSpan, ICompilationProvider provider)
        => GetSymbol(textSpan, provider, "Discord.TextDisplayBuilder");

    private static Result<ICSharpTypeSymbol> GetSymbol(
        CXTextSpan textSpan,
        ICompilationProvider provider,
        string name
    )
    {
        var symbol = provider.GetTypeFromQualifiedName(name);

        if (symbol is null)
        {
            return textSpan.Report(MissingRequiredSymbol(name));
        }

        return new Result<ICSharpTypeSymbol>(symbol);
    }
}