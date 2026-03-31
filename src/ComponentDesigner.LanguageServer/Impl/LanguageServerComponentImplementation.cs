using ComponentDesigner;
using ComponentDesigner.Nodes;
using ComponentDesigner.Nodes.TextControls;

namespace Discord.ComponentDesigner.LanguageServer;

public sealed class LanguageServerComponentImplementation : IComponentImplementation
{
    public static readonly LanguageServerComponentImplementation Instance = new();

    public string Name => "LSP";

    public IComponentRenderer Renderer => new DiscordNetRenderer();

    public ITextControlProvider TextControlProvider => DefaultTextControlProvider.Instance;

    public IComponentTypingProvider? ComponentTypingProvider => null; // TODO

    public ComponentPropertyValueKind? GetPropertyKindOverload(
        IComponentNode component,
        ComponentProperty property
    )
    {
        return null;
    }

    public bool TryAnalyzeNumberOfValues(
        IComponentContext context,
        IComponentNode component,
        ComponentPropertyValue propertyValue,
        CancellationToken cancellationToken,
        out StaticRange range
    )
    {
        range = StaticRange.Empty;
        return false;
    }
}