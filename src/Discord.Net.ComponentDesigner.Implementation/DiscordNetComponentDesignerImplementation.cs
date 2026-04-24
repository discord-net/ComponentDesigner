using ComponentDesigner;
using ComponentDesigner.Nodes;
using ComponentDesigner.Nodes.TextControls;

namespace Discord;

public sealed partial class DiscordNetComponentDesignerImplementation :
    IComponentImplementation
{
    public static readonly DiscordNetComponentDesignerImplementation Instance = new();
    
    public string Name => "Discord.Net";

    public ITextControlProvider TextControlProvider => DefaultTextControlProvider.Instance;

    public IComponentTypingProvider ComponentTypingProvider { get; } = new ComponentTyping();

    public IComponentExtensionProvider? ComponentExtensionProvider => ComponentExtensions.Instance;
}