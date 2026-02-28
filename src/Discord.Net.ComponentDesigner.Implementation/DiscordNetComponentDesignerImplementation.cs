using ComponentDesigner;
using ComponentDesigner.Nodes.TextControls;

namespace Discord;

public sealed class DiscordNetComponentDesignerImplementation :
    IComponentImplementation
{
    public string Name => "Discord.Net";

    public IComponentRenderer Renderer { get; } = new DiscordNetRenderer();

    public ITextControlProvider TextControlProvider => DefaultTextControlProvider.Instance;

    public IComponentTypingProvider ComponentTypingProvider { get; } = new ComponentTyping();


}