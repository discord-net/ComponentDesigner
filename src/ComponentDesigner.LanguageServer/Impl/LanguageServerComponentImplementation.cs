using ComponentDesigner;
using ComponentDesigner.Json;
using ComponentDesigner.Nodes;
using ComponentDesigner.Nodes.TextControls;

namespace Discord.ComponentDesigner.LanguageServer;

public sealed class LanguageServerComponentImplementation : IComponentImplementation
{
    public static readonly LanguageServerComponentImplementation Instance = new();

    public string Name => "LSP";
    public ITextControlProvider TextControlProvider  { get; }

    public IComponentTypingProvider? ComponentTypingProvider  { get; }

    public IComponentExtensionProvider? ComponentExtensionProvider  { get; }

    public LanguageServerComponentImplementation()
    {
        TextControlProvider = DefaultTextControlProvider.Instance;
        ComponentTypingProvider = null;
        ComponentExtensionProvider = null;
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