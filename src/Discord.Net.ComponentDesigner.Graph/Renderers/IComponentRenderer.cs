using Discord.CX.Nodes;

namespace Discord.CX;

public interface IComponentRenderer
{
    string Name { get; }

    bool IsValidComponentType(IComponentContext context, ICSharpTypeSymbol? symbol, CancellationToken cancellationToken = default);
    
    Result<string> RenderContainer(
        IRendererContext context,
        ContainerComponentNode container,
        ComponentState state,
        ComponentPropertyValue id,
        ComponentPropertyValue accentColor,
        ComponentPropertyValue isSpoiler,
        CancellationToken cancellationToken = default
    );

    Result<string> RenderTextDisplay(
        IRendererContext context,
        TextDisplayComponentNode textDisplay,
        TextDisplayState state,
        ComponentPropertyValue id,
        ComponentPropertyValue content,
        CancellationToken cancellationToken = default
    );
}