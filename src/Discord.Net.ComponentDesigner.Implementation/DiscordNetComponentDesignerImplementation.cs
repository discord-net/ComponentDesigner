using ComponentDesigner;
using ComponentDesigner.Nodes;
using ComponentDesigner.Nodes.TextControls;

namespace Discord;

public sealed partial class DiscordNetComponentDesignerImplementation :
    IComponentImplementation
{
    public static readonly DiscordNetComponentDesignerImplementation Instance = new();
    
    public string Name => "Discord.Net";

    public IComponentRenderer Renderer { get; } = new DiscordNetRenderer();

    public ITextControlProvider TextControlProvider => DefaultTextControlProvider.Instance;

    public IComponentTypingProvider ComponentTypingProvider { get; } = new ComponentTyping();

    public ComponentPropertyValueKind? GetPropertyKindOverload(
        IComponentNode component,
        ComponentProperty property
    )
    {
        return null;
    }
}