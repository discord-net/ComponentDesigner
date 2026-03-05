using ComponentDesigner;
using ComponentDesigner.Nodes.TextControls;

namespace Discord.ComponentDesigner.LanguageServer;

public sealed class LanguageServerComponentImplementation : IComponentImplementation
{
    public static readonly LanguageServerComponentImplementation Instance = new();
    
    public string Name => "LSP";

    public IComponentRenderer Renderer => new DiscordNetRenderer();

    public ITextControlProvider TextControlProvider => DefaultTextControlProvider.Instance;

    public IComponentTypingProvider? ComponentTypingProvider => null; // TODO
}