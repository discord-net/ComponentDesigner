using ComponentDesigner;
using ComponentDesigner.Nodes.TextControls;

namespace Discord;

public sealed class DiscordNetComponentDesignerImplementation :
    IComponentImplementation,
    IComponentTypingProvider
{
    public string Name => "Discord.Net";

    public IComponentRenderer Renderer { get; } = new DiscordNetRenderer();

    public ITextControlProvider TextControlProvider => DefaultTextControlProvider.Instance;

    public IComponentTypingProvider ComponentTypingProvider => this;

    public bool IsValidComponentType(
        IComponentContext context,
        ICSharpTypeSymbol? symbol,
        CancellationToken cancellationToken = default
    ) => ComponentBuilderType.TryGetFromSymbol(symbol, context.CompilationProvider, cancellationToken, out _);
}