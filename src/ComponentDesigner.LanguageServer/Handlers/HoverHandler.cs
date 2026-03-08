using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Discord.ComponentDesigner.LanguageServer;

public sealed class HoverHandler : HoverHandlerBase
{
    private readonly ILogger<HoverHandler> _logger;

    public HoverHandler(ILogger<HoverHandler> logger)
    {
        _logger = logger;
    }
    
    protected override HoverRegistrationOptions CreateRegistrationOptions(
        HoverCapability capability,
        ClientCapabilities clientCapabilities
    ) => new HoverRegistrationOptions()
    {
    };
    
    public override async Task<Hover?> Handle(HoverParams request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Got hover request for {Uri}", request.TextDocument.Uri);
        
        if (!DocumentHandler.TryGetDocument(request.TextDocument.Uri, out var document))
        {
            _logger.LogWarning("Got hover request for unknown document {Uri}", request.TextDocument.Uri);
            return null;
        }

        var graph = document.GetGraph(cancellationToken);
        var offset = document.GetSourceOffsetFromPosition(request.Position);

        return HoverProvider.Get(graph, offset);
    }
}